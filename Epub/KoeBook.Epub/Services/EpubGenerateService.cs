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

        var document = _documentStoreService.Documents.Single(d => d.Id == bookScripts.BookProperties.Id);
        var tmpMp3Path = Path.Combine(tempDirectory, "temp.mp3");

        foreach (var scriptLine in bookScripts.ScriptLines)
        {
            var wavData = await _soundGenerationService.GenerateLineSoundAsync(scriptLine, bookScripts.Options, cancellationToken).ConfigureAwait(false);
            var ms = new MemoryStream();
            ms.Write(wavData);
            ms.Position = 0;
            using var reader = new WaveFileReader(ms);
            MediaFoundationEncoder.EncodeToMp3(reader, tmpMp3Path);
            scriptLine.Audio = new Audio(File.ReadAllBytes(tmpMp3Path));
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
