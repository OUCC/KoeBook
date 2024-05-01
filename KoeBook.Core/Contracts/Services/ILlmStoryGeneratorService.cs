using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

internal interface ILlmStoryGeneratorService
{
    ValueTask<string> GenerateStoryAsync(StoryGenre storyGenre, string premise, CancellationToken cancellationToken);
}
