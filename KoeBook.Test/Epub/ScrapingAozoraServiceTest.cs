using System.Runtime.CompilerServices;
using AngleSharp;
using AngleSharp.Dom;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Net.Http;

namespace KoeBook.Test.Epub;

public class ScrapingAozoraServiceTest
{
    private static readonly EpubDocument EmptySingleParagraph = new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph()] }] }] };

    public static object[][] ProcessChildrenTestCases()
    {
        (string, EpubDocument, EpubDocument)[] cases = [
            // レイアウト
            // 1.1 改丁
            (ToMainText(@"<span class=""notes"">［＃改丁］</span>"), EmptySingleParagraph , new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph() { Text = "［＃改丁］", ScriptLine = new Core.Models.ScriptLine("", "", "") }] }] }] }),
        ];
        return cases.Select(c => new object[] { c.Item1, c.Item2, c.Item3 }).ToArray();
    }

    /// <summary>
    /// (htmlの要素の)テキストを"<div class = \"main_text\"></div>"で囲む
    /// </summary>
    /// <param name="text">divタグで囲むhtmlの要素</param>
    /// <returns>divタグで囲まれた<paramref name="text"/></returns>
    private static string ToMainText(string text)
    {
        return @$"<div class = ""main_text"">{text}</div>";
    }

    [Theory]
    [MemberData(nameof(ProcessChildrenTestCases))]
    public async void ProcessChildrenTest(string html, EpubDocument initial, EpubDocument expexted)
    {
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(request => request.Content(html));
        var mainText = doc.QuerySelector(".main_text");
        var scraper = new ScrapingAozoraService(new SplitBraceService(), new ScrapingClientService(new httpClientFactory(), TimeProvider.System));
        scraper._document() = initial;

        scraper.ProcessChildren(mainText);

        Assert.True(HaveSmaeText(scraper._document(), expexted));
    }

    /// <summary>
    /// 2つのEpubdocumentの内容(Guidを除く)内容が一致するかを判定する。
    /// </summary>
    /// <param name="document">比較するEpubdocument</param>
    /// <param name="comparison">比較するEpubdocument</param>
    /// <returns></returns>
    private static bool HaveSmaeText(EpubDocument document, EpubDocument comparison)
    {
        bool same = true;

        same = (document.Title == comparison.Title);
        same = (document.Author == comparison.Author);
        same = (document.CssClasses == comparison.CssClasses);

        foreach ((Chapter selfChapter, Chapter comparisonChapter) in document.Chapters.Zip(comparison.Chapters))
        {
            same = (selfChapter.Title == comparisonChapter.Title);

            foreach ((Section selfSection, Section comparisonSection) in selfChapter.Sections.Zip(comparisonChapter.Sections))
            {
                same = (selfSection.Title == comparisonSection.Title);

                same = selfSection.Elements.Equals(comparisonSection.Elements);
            }
        }

        return same;
    }

    internal class httpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return httpClient;
        }

        private static readonly HttpClient httpClient = new HttpClient();

    }


    [Theory]
    [InlineData("", "")]
    public async Task TextProcess(string input, string expected)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(input));
        Assert.NotNull(doc.ParentElement);

        var result = ScrapingAozora.TextProcess(null, doc.ParentElement!);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", new[] { "" })]
    public async Task AddParagraphs1(string input, string[] expected)
    {
        using var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(input));
        Assert.NotNull(doc.ParentElement);
        var epubDocument = new EpubDocument("title", "author", "", default)
        {
            Chapters = [new() { Sections = [new("section title") { Elements = [new Paragraph() { Text = "test" }] }] }]
        };

        Assert.Equal(expected.Length, epubDocument.Chapters[0].Sections[0].Elements.Count);
        Assert.All(epubDocument.Chapters[0].Sections[0].Elements.Zip(expected), v =>
        {
            var (element, expected) = v;
            var paragraph = Assert.IsType<Paragraph>(element);
            Assert.Equal(expected, paragraph.Text);
        });
    }
}

file static class ScrapingAozora
{
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern string TextProcess(ScrapingAozoraService? _, IElement element);

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    public static extern void AddParagraphs(ScrapingAozoraService service, List<KoeBook.Epub.Models.Element> focusElements, IElement element, bool lastEmpty);

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    public static extern void AddParagraphs(ScrapingAozoraService service, List<KoeBook.Epub.Models.Element> focusElements, string input, bool lastEmpty);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern string TextReplace(ScrapingAozoraService? _, string text);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern (List<int> contentsIds, bool hasChapter, bool hasSection) LoadToc(ScrapingAozoraService? _, IDocument doc, EpubDocument epubDocument);

    [UnsafeAccessor(UnsafeAccessorKind.Field)]
    public static extern ref EpubDocument _document(this ScrapingAozoraService scraper);
}
