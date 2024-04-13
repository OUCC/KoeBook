using System.Collections.Immutable;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;

namespace KoeBook.Epub.Services;

public class ScraperSelectorService(IEnumerable<IScrapingService> scrapingServices) : IScraperSelectorService
{
    private readonly ImmutableArray<IScrapingService> _scrapingServices = scrapingServices.ToImmutableArray();

    public bool IsMatchSites(string url)
    {
        try
        {
            var uri = new Uri(url);
            return _scrapingServices.Any(service => service.IsMatchSite(uri));
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    public async ValueTask<EpubDocument> ScrapingAsync(string url, string coverFillePath, string tempDirectory, Guid id, CancellationToken ct)
    {
        var uri = new Uri(url);

        foreach (var service in _scrapingServices)
        {
            if (service.IsMatchSite(uri))
                return await service.ScrapingAsync(url, coverFillePath, tempDirectory, id, ct);
        }

        throw new ArgumentException("対応するURLではありません");
    }
}
