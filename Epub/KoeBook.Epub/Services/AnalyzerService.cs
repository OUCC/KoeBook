using System.Diagnostics;
using System.Text.RegularExpressions;
using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using KoeBook.Models;

namespace KoeBook.Epub.Services;

public partial class AnalyzerService(
    IScraperSelectorService scrapingService,
    IEpubDocumentStoreService epubDocumentStoreService,
    ILlmAnalyzerService llmAnalyzerService,
    AiStoryAnalyzerService aiStoryAnalyzerService) : IAnalyzerService
{
    private readonly IScraperSelectorService _scrapingService = scrapingService;
    private readonly IEpubDocumentStoreService _epubDocumentStoreService = epubDocumentStoreService;
    private readonly ILlmAnalyzerService _llmAnalyzerService = llmAnalyzerService;
    private readonly AiStoryAnalyzerService _aiStoryAnalyzerService = aiStoryAnalyzerService;

    public async ValueTask<BookScripts> AnalyzeAsync(BookProperties bookProperties, string tempDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(tempDirectory);
        var coverFilePath = Path.Combine(tempDirectory, "Cover.png");
        using var fs = File.Create(coverFilePath);
        await fs.WriteAsync(CoverFile.ToArray(), cancellationToken);
        await fs.FlushAsync(cancellationToken);

        var rubyReplaced = false;
        EpubDocument document;
        try
        {
            switch (bookProperties)
            {
                case { SourceType: SourceType.Url or SourceType.FilePath, Source: string uri }:
                    document = await _scrapingService.ScrapingAsync(uri, coverFilePath, tempDirectory, bookProperties.Id, cancellationToken);
                    break;
                case { SourceType: SourceType.AiStory, Source: AiStory aiStory }:
                    document = _aiStoryAnalyzerService.CreateEpubDocument(aiStory, bookProperties.Id);
                    rubyReplaced = true;
                    break;
                default:
                    throw new UnreachableException($"SourceType: {bookProperties.SourceType}, Source: {bookProperties.Source}");
            }
        }
        catch (EbookException) { throw; }
        catch (Exception ex)
        {
            throw new EbookException(ExceptionType.WebScrapingFailed, innerException: ex);
        }
        _epubDocumentStoreService.Register(document, cancellationToken);

        var scriptLines = document.Chapters.SelectMany(c => c.Sections)
            .SelectMany(s => s.Elements)
            .OfType<Paragraph>()
            .Select<Paragraph, ScriptLine>(rubyReplaced
            ? p => p.ScriptLine!
            : p =>
            {
                // ルビを置換
                var line = ReplaceBaseTextWithRuby(p.Text);

                return p.ScriptLine = new ScriptLine(line, "", "");
            }).Where(l => !string.IsNullOrEmpty(l.Text))
            .ToArray();

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
