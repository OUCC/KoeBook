using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Core.Services;

public partial class ClaudeAnalyzerService(IClaudeService claudeService, IDisplayStateChangeService displayStateChangeService) : ILlmAnalyzerService
{
    private readonly IClaudeService _claudeService = claudeService;
    private readonly IDisplayStateChangeService _displayStateChangeService = displayStateChangeService;

    public async ValueTask<BookScripts> LlmAnalyzeScriptLinesAsync(BookProperties bookProperties, List<ScriptLine> scriptLines, CancellationToken cancellationToken)
    {
        var lineNumberingText = LineNumbering(scriptLines);
        if (_claudeService.Messages is null)
        {
            throw new EbookException(ExceptionType.ApiKeyNotSet);
        }
        try
        {
            var message1 = await _claudeService.Messages.CreateAsync(new()
            {
                Model = "claude-3-opus-20240229", // you can use Claudia.Models.Claude3Opus string constant
                MaxTokens = 4000,
                Messages = [new() {
                        Role = "user",
                        Content = CreatePrompt1(lineNumberingText)
                    }]
            },
                cancellationToken: cancellationToken
            );
            var (characterList, voiceIds) = ExtractCharacterListAndVoiceIds(message1.ToString());

            var message2 = await _claudeService.Messages.CreateAsync(new()
            {
                Model = "claude-3-opus-20240229", // you can use Claudia.Models.Claude3Opus string constant
                MaxTokens = 4000,
                Messages = [new() {
                        Role = "user",
                        Content = CreatePrompt2(voiceList)
                    }]
            },
                cancellationToken: cancellationToken
            );

            var characterVoiceMapping = ExtractCharacterVoiceMapping(message2.ToString());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed);
        }
    }

    private string CreatePrompt1(string lineNumberingText)
    {
        return $$"""
                {{lineNumberingText}}

                Notes:
                - For narration parts, which are not enclosed in quotation marks, select the narrator.
                - For dialogues enclosed in quotation marks, assign a voice other than the narrator.
                - In the character description, include the appropriate voice characteristics.
                Tasks: Based on the notes above, perform the following two tasks:
                - List of characters Objective: To understand the characters appearing in the text, list the character ID, name, and description for all characters who speak at least one line.
                - Output the speaking character or narrator for each line Objective: To identify which character is speaking in each line of the text, output the speaking character or narrator for all lines. Carefully recognize the context and output with attention. Select only one character_id per line.
                - Revise CHARACTER LIST and VOICE ID (in the event that the CHARACTER LIST is incomplete)
                Output Format:
                [CHARACTER LIST]
                c0. ナレーター: The person who speaks the narration parts. A calm-toned male voice.
                c1. {character_name}: {character_description}
                {character_id}. {character_name}: {character_description}
                ...
                [VOICE ID]
                1. {character_id} {narration|dialogue}
                2. {character_id} {narration|dialogue}
                3. {character_id} {narration|dialogue}
                ...
                [REVISE CHARACTER LIST]
                c0. ナレーター: The person who speaks the narration parts. A calm-toned male voice.
                c1. {character_name}: {character_description}
                {character_id}. {character_name}: {character_description}
                ...
                [REVISE VOICE ID]
                1. {character_id} {narration|dialogue}
                2. {character_id} {narration|dialogue}
                3. {character_id} {narration|dialogue}
                ...
                """;
    }

    private string CreatePrompt2(List<ScriptLine> scriptLines, List<Character> characterList, List<string> voiceIds)
    {
        return $$"""
                
    }

    private string LineNumbering(List<ScriptLine> scriptLines)
    {
        var sb = new StringBuilder();
        foreach (var (index, scriptLine) in scriptLines.Select((x, i) => (i, x)))
        {
            sb.AppendLine($"{index + 1}. {scriptLine.Text}");
        }
        return sb.ToString();
    }

    (List<Character>, List<string>) ExtractCharacterListAndVoiceIds(string response)
    {
        var characterList = new List<Character>();
        var voiceIds = new List<string>();
        var lines = response.Split("\n");
        var characterListStartIndex = 0;
        var characterListEndIndex = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("[REVISE CHARACTER LIST]"))
            {
                characterListStartIndex = i + 1;
            }

            if (lines[i].StartsWith("[REVISE VOICE ID]"))
            {
                characterListEndIndex = i;
            }
        }
        for (var i = characterListStartIndex; i < characterListEndIndex; i++)
        {
            var line = lines[i];
            if (line.StartsWith('c'))
            {
                var characterId = line[1..line.IndexOf('.')];
                var characterName = line[(line.IndexOf('.') + 2)..line.IndexOf(':')];
                var characterDescription = line[(line.IndexOf(':') + 2)..];
                characterList.Add(new Character(characterId, characterName, characterDescription));
            }
        }

        {
            var dest = (stackalloc Range[4]);
            for (var i = characterListEndIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i].AsSpan();
                if (line.Length > 0 && line.Contains('c'))
                {
                    if (line.Split(dest, ' ') > 3)
                        throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed);
                    var characterId = line[dest[1]];
                    var narrationOrDialogue = line[dest[2]];
                }
            }
        }
        return (characterList, voiceIds);
    }

    private record Character(string Id, string Name, string Description);
}
