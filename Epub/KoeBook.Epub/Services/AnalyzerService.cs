using System.Text.RegularExpressions;
using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;

namespace KoeBook.Epub.Services;

public partial class AnalyzerService(IScraperSelectorService scrapingService, IEpubDocumentStoreService epubDocumentStoreService, ILlmAnalyzerService llmAnalyzerService) : IAnalyzerService
{
    private readonly IScraperSelectorService _scrapingService = scrapingService;
    private readonly IEpubDocumentStoreService _epubDocumentStoreService = epubDocumentStoreService;
    private readonly ILlmAnalyzerService _llmAnalyzerService = llmAnalyzerService;

    public async ValueTask<BookScripts> AnalyzeAsync(BookProperties bookProperties, string tempDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDirectory);
        var coverFilePath = Path.Combine(tempDirectory, "Cover.png");
        using var fs = File.Create(coverFilePath);
        await fs.WriteAsync(CoverFile.ToArray(), cancellationToken);
        await fs.FlushAsync(cancellationToken);

        EpubDocument? document;
        try
        {
            document = await _scrapingService.ScrapingAsync(bookProperties.Source, coverFilePath, tempDirectory, bookProperties.Id, cancellationToken);
        }
        catch (EbookException)
        {
            throw;
        }
        catch (Exception ex)
        {
            EbookException.Throw(ExceptionType.WebScrapingFailed, innerException: ex);
            return default;
        }
        _epubDocumentStoreService.Register(document, cancellationToken);

        var scriptLines = document.Chapters.SelectMany(c => c.Sections)
            .SelectMany(s => s.Elements)
            .OfType<Paragraph>()
            .Select(p =>
            {
                // ルビを置換
                var line = ReplaceBaseTextWithRuby(p.Text);

                return p.ScriptLine = new ScriptLine(line, "", "");
            }).ToList();

        // LLMによる話者、スタイル解析
        var bookScripts = await _llmAnalyzerService.LlmAnalyzeScriptLinesAsync(bookProperties, scriptLines, cancellationToken)!;

        return bookScripts;
    }

    private static string ReplaceBaseTextWithRuby(string text)
    {
        // 元のテキストからルビタグをすべてルビテキストに置き換える
        return RubyRegex().Replace(text, m => m.Groups[2].Value);
    }

    [GeneratedRegex(@"<ruby>\s*<rb>(.*?)</rb>\s*<rp>\s*[（《\(]\s*</rp>\s*<rt>(.*?)</rt>\s*<rp>\s*[）》\)]\s*</rp>\s*</ruby>", RegexOptions.Multiline)]
    private static partial Regex RubyRegex();
}
