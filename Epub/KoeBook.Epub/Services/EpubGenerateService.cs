using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using NAudio.Wave;

namespace KoeBook.Epub.Services;

public class EpubGenerateService(ISoundGenerationService soundGenerationService, IEpubDocumentStoreService epubDocumentStoreService, IEpubCreateService epubCreateService) : IEpubGenerateService
{
    private readonly ISoundGenerationService _soundGenerationService = soundGenerationService;
    private readonly IEpubDocumentStoreService _documentStoreService = epubDocumentStoreService;
    private readonly IEpubCreateService _createService = epubCreateService;

    public async ValueTask<string> GenerateEpubAsync(BookScripts bookScripts, string tempDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var document = _documentStoreService.Documents.Single(d => d.Id == bookScripts.BookProperties.Id);

        for (var i = 0; i < bookScripts.ScriptLines.Length; i++)
        {
            var scriptLine = bookScripts.ScriptLines[i];
            scriptLine.Audio = await _soundGenerationService.GenerateLineSoundAsync(scriptLine, bookScripts.Options, cancellationToken).ConfigureAwait(false);
        }

        if (await _createService.TryCreateEpubAsync(document, tempDirectory, cancellationToken).ConfigureAwait(false))
        {
            await _documentStoreService.UnregisterAsync(bookScripts.BookProperties.Id);
            return Path.Combine(tempDirectory, $"{bookScripts.BookProperties.Id}.epub");
        }
        else
        {
            throw new EbookException(ExceptionType.EpubCreateError);
        }
    }
}
