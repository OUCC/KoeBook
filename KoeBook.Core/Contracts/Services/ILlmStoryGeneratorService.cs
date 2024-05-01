using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

public interface ILlmStoryGeneratorService
{
    ValueTask<string> GenerateStoryAsync(StoryGenre storyGenre, string premise, CancellationToken cancellationToken);
}
