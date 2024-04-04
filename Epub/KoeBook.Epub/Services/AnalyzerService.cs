using System.Text;
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
    private Dictionary<string, string> _rubyReplacements = new Dictionary<string, string>();

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

        // 800文字以上になったら１チャンクに分ける
        var chunks = new List<string>();
        var chunk = new StringBuilder();
        foreach (var line in scriptLines)
        {
            if (chunk.Length + line.Text.Length > 800)
            {
                chunks.Add(chunk.ToString());
                chunk.Clear();
            }
            chunk.AppendLine(line.Text);
        }
        if (chunk.Length > 0) chunks.Add(chunk.ToString());

        // GPT4による話者、スタイル解析
        var bookScripts = await _llmAnalyzerService.LlmAnalyzeScriptLinesAsync(bookProperties, scriptLines, chunks, cancellationToken);

        return bookScripts;
    }

    private static string ReplaceBaseTextWithRuby(string text)
    {
        // 元のテキストからルビタグをすべてルビテキストに置き換える
        return RubyRegex().Replace(text, m => m.Groups[2].Value);
    }

    [GeneratedRegex("<ruby><rb>(.*?)</rb><rp>（</rp><rt>(.*?)</rt><rp>）</rp></ruby>")]
    private static partial Regex RubyRegex();
}
