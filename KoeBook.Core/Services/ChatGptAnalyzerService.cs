using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Helpers;
using KoeBook.Core.Models;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;

namespace KoeBook.Core.Services;

public partial class ChatGptAnalyzerService(IOpenAIService openAIService, IDisplayStateChangeService displayStateChangeService) : ILlmAnalyzerService
{
    private readonly IOpenAIService _openAiService = openAIService;
    private readonly IDisplayStateChangeService _displayStateChangeService = displayStateChangeService;

    public async ValueTask<BookScripts> LlmAnalyzeScriptLinesAsync(BookProperties bookProperties, ScriptLine[] scriptLines, CancellationToken cancellationToken)
    {
        var chunks = new List<string>();
        var chunk = new StringBuilder();
        foreach (var line in scriptLines)
        {
            if (chunk.Length + line.Text.Length > 800)
            {
                chunks.Add(chunk.ToString());
                chunk.Clear();
            }
            chunk.AppendLine(line.Text);
        }
        if (chunk.Length > 0) chunks.Add(chunk.ToString());
        var progress = _displayStateChangeService.ResetProgress(bookProperties, GenerationState.Analyzing, chunks.Count);
        Queue<string> summaryList = new();
        Queue<string> characterList = new();
        int currentLineIndex = 0;
        for (int i = 0; i < chunks.Count; i++)
        {
            // 話者・スタイル解析
            var summary = "";
            var characters = "";
            if (summaryList.Count != 0)
            {
                summary = summaryList.Peek();
                characters = characterList.Peek();
            }
            var Task1 = CharacterStyleAnalysisAsync(scriptLines, chunks, summary, characters, i, currentLineIndex, cancellationToken);
            // 要約・キャラクターリスト解析
            var summary1 = "";
            var characters1 = "";
            if (summaryList.Count > 5)
            {
                summary1 = summaryList.Dequeue();
                characters1 = characterList.Dequeue();
            }
            var Task2 = SummaryCharacterListAnalysisAsync(chunks, summary1, characters1, i, cancellationToken);
            // WhenAllで非同期処理を待つ
            await Task.WhenAll(Task1, Task2);
            currentLineIndex += chunks[i].Split("\n").Length - 1;
            // 結果をキューに追加
            summaryList.Enqueue(Task2.Result.summary);
            characterList.Enqueue(Task2.Result.characterList);
            progress.UpdateProgress(i + 1);
        }

        // キャラクター名と声のマッピング
        var characterVoiceMapping = await GetCharacterVoiceMappingAsync(scriptLines, characterList.Peek(), cancellationToken);

        var bookScripts = new BookScripts
        (
            bookProperties,
            new BookOptions
            {
                CharacterMapping = characterVoiceMapping
            }
        )
        {
            ScriptLines = [.. scriptLines]
        };
        return bookScripts;
    }

    private async Task CharacterStyleAnalysisAsync(ScriptLine[] scriptLines,
                                                   List<string> chunks,
                                                   string summary,
                                                   string characterList,
                                                   int idx,
                                                   int currentLineIndex,
                                                   CancellationToken cancellationToken)
    {
        int count = 0;
RESTART:
        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages =
            [
                ChatMessage.FromSystem($$"""
                    All Information
                    - Goal
                    - Information about this story
                    - Output Format
                    - Notes


                    # Goal
                    Set speaker name and style of target sentence


                    # Information about this story
                    1. Summary so far
                    {{summary}}


                    2. Character List
                    {{characterList}}


                    3. Target Sentence
                    {{chunks[idx]}}


                    # Output Format
                    ```
                    #### CharacterList
                    - name1
                    - name2
                    ...


                    #### Summery
                    - point1
                    - point2
                    - point3


                    #### Talker and Style Setting
                    {SENTENCE}[{TALKER}:{STYLE}]
                    {SENTENCE}[{TALKER}:{STYLE}]
                    ...
                    {SENTENCE}[{TALKER}:{STYLE}]
                    ```
                    STYLE = narration | neutral | laughing | happy | sad | cry | surprised | angry
                    assign the name of the speaker to {TALKER} if the sentence is dialogue, or use 'ナレーション' for narrative sentences. If the sentence is part of a monologue, use the name of the character who is speaking.
                    {SENTENCE} should be used as is from the first line of the "Target Sentence" section, without a line break.


                    # **Notes**
                    ## **Separate Narration and lines.** Be careful as you often make mistakes!!
                    Input
                    ```
                    そうして、時が流れ少し肌寒い季節となった。木もすっかりやせ細っていて、黄金色の葉っぱが雪のように降っている。落ちてくる落ち葉を見て、清美は言った。
                    「悲しいね…。あなたとの思い出が、一枚一枚、地面に落ちていくみたい…。」
                    ```
                    Output
                    ```
                    そうして、時が流れ少し肌寒い季節となった。木もすっかりやせ細っていて、黄金色の葉っぱが雪のように降っている。落ちてくる落ち葉を見て、清美は言った。[ナレーション:narration]
                    「悲しいね…。あなたとの思い出が、一枚一枚、地面に落ちていくみたい…。」[漆原清美:sad]
                    ```
                    ```
                    """
                    )
            ],
            Model = OpenAI.ObjectModels.Models.Gpt_4_turbo_preview,
            MaxTokens = 4000
        }, cancellationToken: cancellationToken);
        if (completionResult.Successful)
        {
            var result = completionResult.Choices.First().Message.Content;
            // "#### Talker and Style Setting"以下の文章を改行区切りでリスト化
            var output = new List<string>();
            var lines = result?.Split("\n");
            var start = false;
            for (var i = 0; i < lines?.Length; i++)
            {
                var line = lines[i];
                if (start)
                {
                    if (line.Contains("```"))
                    {
                        break;
                    }
                    output.Add(line);
                }
                if (line.Contains("#### Talker and Style Setting"))
                {
                    start = true;
                }
            }
            // パラグラフと対応するoutputをマッピング
            for (int i = 0; i < output.Count; i++)
            {
                // {SENTENCE}[{TALKER}:{STYLE}]の形式になっているかチェック
                var match = Regex.Match(output[i], @"(.+)\[(.+):(.+)]");
                if (match.Success)
                {
                    var sentence = match.Groups[1].Value;
                    var talker = match.Groups[2].Value;
                    var style = match.Groups[3].Value;
                    if (sentence == scriptLines[currentLineIndex + i].Text || Math.Abs(sentence.Length - scriptLines[currentLineIndex + i].Text.Length) < 5)
                    {
                        scriptLines[currentLineIndex + i].Character = talker;
                        scriptLines[currentLineIndex + i].Style = style;
                    }
                    else if (count > 3)
                    {
                        EbookException.Throw(ExceptionType.Gpt4TalkerAndStyleSettingFailed);
                    }
                    else
                    {
                        count++;
                        goto RESTART;
                    }
                }
            }
        }
    }

    private async Task<(string summary, string characterList)> SummaryCharacterListAnalysisAsync(
                                                         List<string> chunks,
                                                         string summary,
                                                         string characterList,
                                                         int idx,
                                                         CancellationToken cancellationToken)
    {
        var storyText = string.Join("\n", chunks.Skip(int.Max(0, idx - 4)).Take(4));
        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages =
            [
                ChatMessage.FromSystem($$"""
                    All Information
                    - Goal
                    - Information about this story
                    - Output Format


                    # Goal
                    Make CharacterList and Summery of the story.


                    # Information about this story
                    1. Character List
                    {{characterList}}


                    2. Summary so far
                    {{summary}}


                    3. Story
                    {{storyText}}


                    # Output Format (write name,description,summary in Japanese!!)
                    ```
                    #### CharacterList
                    {name1}
                    - {description1}
                    - {description2}
                    ...(up to 10 descriptions)


                    {name2}
                    - {description1}
                    - {description2}
                    ...
                    ...


                    #### Summery of {{Math.Min(20, (idx + 1) * 5)}} points
                    - {summary1}
                    - {summary2}
                    ...
                    ```
                    """),
            ],
            Model = OpenAI.ObjectModels.Models.Gpt_4_turbo_preview,
            MaxTokens = 4000
        }, cancellationToken: cancellationToken);
        if (completionResult.Successful)
        {
            var result = completionResult.Choices.First().Message.Content;
            var lines = result?.Split("\n");
            bool summaryStart = false;
            bool characterListStart = false;
            StringBuilder _summary = new();
            StringBuilder _characterList = new();

            for (var i = 0; i < lines?.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("#### CharacterList"))
                {
                    characterListStart = true;
                    continue;
                }
                if (line.Contains("#### Summery of"))
                {
                    summaryStart = true;
                    characterListStart = false;
                    continue;
                }
                if (characterListStart)
                {
                    _characterList.AppendLine(line);
                }
                if (summaryStart)
                {
                    if (line.Contains("```"))
                    {
                        break;
                    }
                    _summary.AppendLine(line);
                }
            }

            return (_summary.ToString(), _characterList.ToString());
        }
        else
        {
            EbookException.Throw(ExceptionType.Gpt4TalkerAndStyleSettingFailed);
            return default!;
        }
    }

    private async Task<Dictionary<string, string>> GetCharacterVoiceMappingAsync(ScriptLine[] scriptLines, string characterDescription, CancellationToken cancellationToken)
    {
        // キャラクター名一覧の取得
        var characterList = scriptLines.Select(x => "- " + x.Character).Distinct().ToList();
        var characterListString = string.Join("\n", characterList);
        var completionResult = await _openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages =
            [
                ChatMessage.FromSystem($$"""
                All Information
                    - Goal
                    - Character Description
                    - Character List
                    - Voice List
                    - Output Format

                #### Goal
                Make a table of character names and voices

                #### Character Description
                {{characterDescription}}

                #### Character List
                {{characterListString}}

                #### Voice List
                - ナレーション女
                - 女子小学生
                - 女子中高生
                - 成人女性
                - 老年女性
                - ナレーション男
                - 男子小学生
                - 男子中高生
                - 成人男性
                - 老年男性

                #### Output Format
                ```
                {character1} - {voice1}
                {character2} - {voice2}
                ...
                ```
                """)
            ],
            Model = OpenAI.ObjectModels.Models.Gpt_4_turbo_preview,
            MaxTokens = 4000
        }, cancellationToken: cancellationToken);
        if (completionResult.Successful)
        {
            var result = completionResult.Choices.First().Message.Content;
            var lines = result?.Split("\n");
            Dictionary<string, string> characterVoiceMapping = [];
            foreach (var match in from line in lines
                                  let match = CharacterMappingRegex().Match(line)
                                  select match)
            {
                if (match.Success)
                {
                    characterVoiceMapping.Add(match.Groups[1].Value, match.Groups[2].Value);
                }
                else
                {
                    EbookException.Throw(ExceptionType.Gpt4TalkerAndStyleSettingFailed);
                }
            }
            return characterVoiceMapping;
        }
        else
        {
            EbookException.Throw(ExceptionType.Gpt4TalkerAndStyleSettingFailed);
            return default!;
        }
    }

    [GeneratedRegex(@"(.+) - (.+)")]
    private static partial Regex CharacterMappingRegex();
}
