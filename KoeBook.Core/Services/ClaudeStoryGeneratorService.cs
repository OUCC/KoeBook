using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Core.Services;

public class ClaudeStoryGeneratorService(IClaudeService claudeService) : IStoryCreatorService
{
    private readonly IClaudeService _claudeService = claudeService;

    public async ValueTask<string> CreateStoryAsync(StoryGenre storyGenre, string premise, CancellationToken cancellationToken)
    {
        if (_claudeService.Messages is null)
        {
            throw new EbookException(ExceptionType.ApiKeyNotSet);
        }
        try
        {
            var storyXml = await _claudeService.Messages.CreateAsync(new()
            {
                Model = Claudia.Models.Claude3Opus,
                MaxTokens = 4000,
                Temperature = 0.4,
                Messages = [new()
                {
                    Role = "user",
                    Content = CreateStoryPrompt(storyGenre, premise)
                }]
            },
                cancellationToken: cancellationToken
            );
            var xml = storyXml.ToString();
            return xml[xml.IndexOf('<')..(xml.LastIndexOf('>') + 1)];
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e)
        {
            throw new EbookException(ExceptionType.ClaudeTalkerAndStyleSettingFailed, innerException: e);
        }
    }

    private string CreateStoryPrompt(StoryGenre storyGenre, string premise)
    {
        return $"""
            You are a highly capable AI novelist that can write compelling 2500-character short stories and novellas in fluent, natural Japanese based on a given theme or plot points.

            When crafting the story, please focus on the following:
            - Use dialogue extensively to advance the plot while revealing characters' personalities, motivations and relationships
            - Write witty, revealing conversations authentic to each character's voice
            - Vary dialogue tags and phrases to convey nuanced speech
            - Aim to create a polished story where characters' distinct voices come through in the dialogue, within the 2500-character limit
            - Words beyond the general vocabulary and proper nouns that are easily misread are given ruby. Not necessary for simple vocabulary.

            Please generate the full story in Japanese in a single output follow this example:
            <?xml version="1.0" encoding="UTF-8"?>
            <Book>
                <Title>境界線の向こう側</Title>
                <Content>
                    <Section>
                        <Paragraph><Text>高校2年の夏、バレー部のエースで</Text><Ruby><Rb>端正</Rb><Rt>たんせい</Rt></Ruby><Text>な顔立ちの山田祐樹は、バスケ部のキャプテンで凛とした佇まいの田中麻衣に密かに想いを寄せていた。しかし、両者の部活仲間たちの目を</Text><Ruby><Rb>憚</Rb><Rt>はばか</Rt></Ruby><Text>り、互いに素振りも見せずにいた。</Text></Paragraph>
                    </Section>
                </Content>
            </Book>

            Based on this prompt, please generate a ~2500-character Japanese story from the specified theme or plot points provided.

            theme: {storyGenre.Genre}
            premise: {premise}
            """;
    }
}
