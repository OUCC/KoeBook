using KoeBook.Epub.Models;

namespace KoeBook.Epub.Contracts.Services;

public interface IScrapingService
{
    public bool IsMatchSite(Uri url);

    public ValueTask<EpubDocument> ScrapingAsync(string url, string tempDirectory, Guid id, CancellationToken ct);
}
