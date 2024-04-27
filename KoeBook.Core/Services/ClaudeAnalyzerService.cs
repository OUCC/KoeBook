using System.Text;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Core.Services;

public partial class ClaudeAnalyzerService(IClaudeService claudeService, IDisplayStateChangeService displayStateChangeService, ISoundGenerationSelectorService soundGenerationSelectorService) : ILlmAnalyzerService
{
    private readonly IClaudeService _claudeService = claudeService;
    private readonly IDisplayStateChangeService _displayStateChangeService = displayStateChangeService;
    private readonly ISoundGenerationSelectorService _soundGenerationSelectorService = soundGenerationSelectorService;

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
                Messages = [new()
                {
                    Role = "user",
                    Content = CreateCharaterGuessPrompt(lineNumberingText)
                }]
            },
                cancellationToken: cancellationToken
            );
            var characterList = ExtractCharacterList(message1.ToString());

            var message2 = await _claudeService.Messages.CreateAsync(new()
            {
                Model = "claude-3-opus-20240229", // you can use Claudia.Models.Claude3Opus string constant
                MaxTokens = 4000,
                Messages = [new()
                {
                    Role = "user",
                    Content = CreateVoiceTypeAnalyzePrompt(characterList)
                }]
            },
                cancellationToken: cancellationToken
            );

            var characterVoiceMapping = ExtractCharacterVoiceMapping(message2.ToString(), characterList);

            return new(bookProperties, new(characterVoiceMapping)) { ScriptLines = scriptLines };
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed);
        }
    }

    private Dictionary<string, string> ExtractCharacterVoiceMapping(string response, List<Character> characterList)
    {
        return response.Split("\n")
           .SkipWhile(l => !l.StartsWith("[Assign Voices]"))
           .Where(l => l.StartsWith('c'))
           .Select(l =>
           {
               var characterId = l[1..l.IndexOf('.')];
               var voiceType = l[(l.IndexOf(':') + 2)..];
               // voiceTypeが_soundGenerationSelectorService.Modelsに含まれているかチェック
               return _soundGenerationSelectorService.Models.Any(x => x.Name == voiceType)
                   ? (characterId, voiceType)
                   : (characterId, string.Empty);
           }).ToDictionary();
    }

    private static string CreateCharaterGuessPrompt(string lineNumberingText)
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

    private string CreateVoiceTypeAnalyzePrompt(List<Character> characterList)
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

    private static string LineNumbering(List<ScriptLine> scriptLines)
    {
        var sb = new StringBuilder();
        foreach (var (index, scriptLine) in scriptLines.Select((x, i) => (i, x)))
        {
            sb.AppendLine($"{index + 1}. {scriptLine.Text}");
        }
        return sb.ToString();
    }

    private static List<Character> ExtractCharacterList(string response)
    {
        var characterList = new List<Character>();
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
                break;
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
        return characterList;
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
