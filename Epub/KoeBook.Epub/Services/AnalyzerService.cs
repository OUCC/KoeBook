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

        var scriptLines = new List<ScriptLine>();
        foreach (var chapter in document.Chapters)
        {
            foreach (var section in chapter.Sections)
            {
                foreach (var element in section.Elements)
                {
                    if (element is Paragraph paragraph)
                    {
                        var line = paragraph.Text;
                        // rubyタグがあればルビのdictionaryに登録
                        var rubyDict = ExtractRuby(line).ToDictionary();

                        foreach (var ruby in rubyDict)
                        {
                            if (!_rubyReplacements.ContainsKey(ruby.Key))
                            {
                                _rubyReplacements.Add(ruby.Key, ruby.Value);
                            }
                        }
                        // ルビを置換
                        line = ReplaceBaseTextWithRuby(line, rubyDict);

                        var scriptLine = new ScriptLine(line, "", "");
                        paragraph.ScriptLine = scriptLine;
                        scriptLines.Add(scriptLine);
                    }
                }
            }
        }

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

    private static IEnumerable<KeyValuePair<string, string>> ExtractRuby(string text)
    {
        return RubyRegex()
            .Matches(text)
            .Select(m => KeyValuePair.Create(m.Groups[1].Value, m.Groups[2].Value));
    }

    private static string ReplaceBaseTextWithRuby(string text, Dictionary<string, string> rubyDict)
    {
        // 元のテキストからルビタグをすべてルビテキストに置き換える
        var resultText = text;
        foreach (var pair in rubyDict)
        {
            var rubyTag = $"<ruby><rb>{pair.Key}</rb><rp>（</rp><rt>{pair.Value}</rt><rp>）</rp></ruby>";
            resultText = resultText.Replace(rubyTag, pair.Value);
        }

        return resultText;
    }

    [GeneratedRegex("<ruby><rb>(.*?)</rb><rp>（</rp><rt>(.*?)</rt><rp>）</rp></ruby>")]
    private static partial Regex RubyRegex();
}
