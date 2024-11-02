using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Epub.Models;

namespace KoeBook.Core.Services.Mocks;

public class SoundGenerationServiceMock : ISoundGenerationService
{
    public async ValueTask<Audio> GenerateLineSoundAsync(ScriptLine scriptLine, BookOptions bookOptions, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        return new Audio(TimeSpan.FromSeconds(0), Stream.Null);
    }
}
