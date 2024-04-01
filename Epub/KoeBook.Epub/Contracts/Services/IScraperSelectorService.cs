using KoeBook.Epub.Models;

namespace KoeBook.Epub.Contracts.Services;

/// <summary>
/// スクレイピングを行い、EpubDocumentを作成します。
/// </summary>
public interface IScraperSelectorService
{
    /// <summary>
    /// 外部URLが処理の対象か調べます
    /// </summary>
    public bool IsMatchSites(string url);

    public ValueTask<EpubDocument> ScrapingAsync(string url, string coverFillePath, string tempDirectory, Guid id, CancellationToken ct);
}
