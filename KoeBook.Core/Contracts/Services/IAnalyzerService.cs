using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

public interface IAnalyzerService
{
    /// <summary>
    /// 本の情報の取得・解析を行います
    /// </summary>
    /// <returns>編集前の読み上げテキスト</returns>
    ValueTask<BookScripts> AnalyzeAsync(BookProperties bookProperties, string tempDirectory, CancellationToken cancellationToken);
}
