using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Epub;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;

namespace KoeBook.Epub.Services;

public class EpubGenerateService(ISoundGenerationService soundGenerationService, IEpubDocumentStoreService epubDocumentStoreService, IEpubCreateService epubCreateService) : IEpubGenerateService
{
    private readonly ISoundGenerationService _soundGenerationService = soundGenerationService;
    private readonly IEpubDocumentStoreService _documentStoreService = epubDocumentStoreService;
    private readonly IEpubCreateService _createService = epubCreateService;

    public async ValueTask<string> GenerateEpubAsync(BookScripts bookScripts, string tempDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = _documentStoreService.Documents.Where(doc => doc.Id == bookScripts.BookProperties.Id).FirstOrDefault()
            ?? throw new InvalidOperationException($"The epub document ({bookScripts.BookProperties.Id}) can't be found.");

        foreach (var scriptLine in bookScripts.ScriptLines)
        {
            scriptLine.Audio = new Audio(await _soundGenerationService.GenerateLineSoundAsync(scriptLine, bookScripts.Options, cancellationToken).ConfigureAwait(false));
        }

        if (await _createService.TryCreateEpubAsync(document, tempDirectory, cancellationToken).ConfigureAwait(false))
        {
            _documentStoreService.Unregister(bookScripts.BookProperties.Id);
            return Path.Combine(tempDirectory, $"{bookScripts.BookProperties.Id}.epub");
        }
        else
        {
            throw new EbookException(ExceptionType.EpubCreateError);
        }
    }
}
