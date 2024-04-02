using System.Net.Http.Headers;

namespace KoeBook.Epub.Contracts.Services;

public interface IScrapingClientService
{
    /// <summary>
    /// スクレイピングでGETする用
    /// APIを叩く際は不要
    /// </summary>
    Task<string> GetAsStringAsync(string url, CancellationToken ct);

    /// <summary>
    /// スクレイピングでGETする用
    /// APIを叩く際は不要
    /// </summary>
    Task<ContentDispositionHeaderValue?> GetAsStreamAsync(string url, Stream destination, CancellationToken ct);

    /// <summary>
    /// ファイルダウンロードを行います
    /// </summary>
    Task DownloadToFileAsync(string url, string destPath, CancellationToken ct);
}
