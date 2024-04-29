using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

public interface IStoryCreaterService
{
    /// <returns>XML</returns>
    public ValueTask<string> CreateStoryAsync(StoryGenre genre, string intruction, CancellationToken cancellationToken);
}
