﻿using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Helpers;
using KoeBook.Core.Models;

namespace KoeBook.Core.Services;

public partial class ClaudeAnalyzerService(IClaudeService claudeService, IDisplayStateChangeService displayStateChangeService, ISoundGenerationSelectorService soundGenerationSelectorService) : ILlmAnalyzerService
{
    private readonly IClaudeService _claudeService = claudeService;
    private readonly IDisplayStateChangeService _displayStateChangeService = displayStateChangeService;
    private readonly ISoundGenerationSelectorService _soundGenerationSelectorService = soundGenerationSelectorService;
    private static readonly SearchValues<char> _searchValues = SearchValues.Create(", ");

    public async ValueTask<BookScripts> LlmAnalyzeScriptLinesAsync(BookProperties bookProperties, ScriptLine[] scriptLines, CancellationToken cancellationToken)
    {
        var progress = _displayStateChangeService.ResetProgress(bookProperties, GenerationState.Analyzing, 2);
        var lineNumberingText = LineNumbering(scriptLines);
        if (_claudeService.Messages is null)
        {
            throw new EbookException(ExceptionType.ApiKeyNotSet);
        }
        try
        {
            var message1 = await _claudeService.Messages.CreateAsync(new()
            {
                Model = Claudia.Models.Claude3Opus,
                MaxTokens = 4000,
                Messages = [new()
                {
                    Role = "user",
                    Content = CreateCharacterGuessPrompt(lineNumberingText)
                }]
            },
                cancellationToken: cancellationToken
            );
            var (characters, characterId2Name) = ExtractCharacterList(message1.ToString(), scriptLines);
            progress.IncrementProgress();

            var message2 = await _claudeService.Messages.CreateAsync(new()
            {
                Model = Claudia.Models.Claude3Opus,
                MaxTokens = 4000,
                Messages = [new()
                {
                    Role = "user",
                    Content = CreateVoiceTypeAnalyzePrompt(characters)
                }]
            },
                cancellationToken: cancellationToken
            );
            var characterVoiceMapping = ExtractCharacterVoiceMapping(message2.ToString(), characterId2Name);
            progress.Finish();

            return new(bookProperties, new(characterVoiceMapping)) { ScriptLines = [.. scriptLines] };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e)
        {
            throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed, innerException: e);
        }
    }

    private Dictionary<string, string> ExtractCharacterVoiceMapping(string response, Dictionary<string, string> characterIdDic)
    {
        return response.Split("\n")
           .SkipWhile(l => !l.StartsWith("[Assign Voices]"))
           .Where(l => l.StartsWith('c'))
           .Select(l =>
           {
               var characterId = l[1..l.IndexOf('.')];
               var voiceTypeSpan = l.AsSpan()[(l.IndexOf(':') + 2)..].Trim();
               // ボイス割り当てが複数あたったときに先頭のものを使う（例：群衆 AdultMan, AdultWoman)
               var separatorIndex = voiceTypeSpan.IndexOfAny(_searchValues);
               if (separatorIndex > 0)
               {
                   voiceTypeSpan = voiceTypeSpan[..separatorIndex];
               }
               // voiceTypeが_soundGenerationSelectorService.Modelsに含まれているかチェック
               var voiceType = voiceTypeSpan.ToString();
               return _soundGenerationSelectorService.Models.Any(x => x.Name == voiceType)
                   ? (characterIdDic[characterId], voiceType)
                   : (characterIdDic[characterId], string.Empty);
           }).ToDictionary();
    }

    private static string CreateCharacterGuessPrompt(string lineNumberingText)
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
                c0. ナレーター: {character_and_voice_description, example:"The person who speaks the narration parts. A calm-toned male voice."}
                c1. {character_name}: {character_and_voice_description}
                {character_id}. {character_name}: {character_and_voice_description}
                ...
                [VOICE ID]
                1. {character_id} {narration|dialogue}
                2. {character_id} {narration|dialogue}
                3. {character_id} {narration|dialogue}
                ...
                [REVISE CHARACTER LIST]
                c0. ナレーター: {character_and_voice_description}
                c1. {character_name}: {character_and_voice_description}
                {character_id}. {character_name}: {character_and_voice_description}
                ...
                [REVISE VOICE ID]
                1. {character_id} {narration|dialogue}
                2. {character_id} {narration|dialogue}
                3. {character_id} {narration|dialogue}
                ...
                """;
    }

    private string CreateVoiceTypeAnalyzePrompt(Character[] characterList)
    {
        return $$"""
            Assign the most fitting voice type to each character from the provided list, ensuring the chosen voice aligns with their role and attributes in the story. Only select from the available voice types.

            Characters:
            {{string.Join("\n", characterList.Select(character => $"c{character.Id}. {character.Name}: {character.Description}"))}}

            Voice Types:
            {{string.Join(",", _soundGenerationSelectorService.Models.Select(m => m.Name))}}

            Output Format:
            [Assign Voices]
            c0. {character_name}: {voice_type}
            c1. {character_name}: {voice_type}
            """;
    }

    private static string LineNumbering(ScriptLine[] scriptLines)
    {
        var sb = new StringBuilder();
        foreach (var (index, scriptLine) in scriptLines.Select((x, i) => (i, x)))
        {
            sb.AppendLine($"{index + 1}. {scriptLine.Text}");
        }
        return sb.ToString();
    }

    private static (Character[], Dictionary<string, string>) ExtractCharacterList(string response, ScriptLine[] scriptLines)
    {
        var lines = response.Split("\n");
        var characters = lines
            .SkipWhile(l => !l.StartsWith("[REVISE CHARACTER LIST]"))
            .TakeWhile(l => !l.StartsWith("[REVISE VOICE ID]"))
            .Where(l => l.StartsWith('c'))
            .Select(l =>
            {
                var dotIndex = l.IndexOf('.');
                var colonIndex = l.IndexOf(':');
                return new Character(l[1..dotIndex], l[(dotIndex + 2)..colonIndex], l[(colonIndex + 2)..]);
            }).ToArray();

        var characterId2Name = characters.Select(x => (x.Id, x.Name)).ToDictionary();
        var voiceIdLinesCount = lines.SkipWhile(l => !l.StartsWith("[REVISE VOICE ID]"))
                                .Where((x, i) => x.StartsWith(i.ToString())) //[REVISE VOICE ID]の分ズレる
                                .Zip(scriptLines)
                                .Select(zippedLine =>
                                {
                                    var voiceIdLine = zippedLine.First.AsSpan();
                                    voiceIdLine = voiceIdLine[(voiceIdLine.IndexOf(' ') + 2)..];//cまで無視
                                    voiceIdLine = voiceIdLine[..voiceIdLine.IndexOf(' ')];// 二人以上話す時には先頭のものを使う
                                    if (voiceIdLine[^1] == '.')// idに"."がつくことがあるので削除する
                                    {
                                        voiceIdLine = voiceIdLine[..^1];
                                    }
                                    if (characterId2Name.TryGetValue(voiceIdLine.ToString(), out var characterName))
                                    {
                                        zippedLine.Second.Character = characterName;
                                    }
                                    else { throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed); }
                                    return 0;
                                }).Count();
        if (voiceIdLinesCount != scriptLines.Length)
            throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed);
        return (characters, characterId2Name);
    }

    private class Character
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string VoiceType { get; set; } = string.Empty;
        public Character(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
        }
    }
}
