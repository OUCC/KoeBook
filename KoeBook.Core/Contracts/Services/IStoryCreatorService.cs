using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

public interface IStoryCreatorService
{
    /// <returns>XML</returns>
    public ValueTask<string> CreateStoryAsync(StoryGenre genre, string instruction, CancellationToken cancellationToken);
}
