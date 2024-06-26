﻿using KoeBook.Core;
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

        for (var i = 0; i < bookScripts.ScriptLines.Length; i++)
        {
            var scriptLine = bookScripts.ScriptLines[i];
            var wavData = await _soundGenerationService.GenerateLineSoundAsync(scriptLine, bookScripts.Options, cancellationToken).ConfigureAwait(false);
            using var ms = new MemoryStream(wavData);
            using var reader = new WaveFileReader(ms);
            var tmpMp3Path = Path.Combine(tempDirectory, $"{document.Title}{i}.mp3");
            MediaFoundationEncoder.EncodeToMp3(reader, tmpMp3Path);
            using var mp3Stream = new Mp3FileReader(tmpMp3Path);
            scriptLine.Audio = new Audio(mp3Stream.TotalTime, tmpMp3Path);
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
