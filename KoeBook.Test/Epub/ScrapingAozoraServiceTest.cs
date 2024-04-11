using System.Runtime.CompilerServices;
using AngleSharp;
using AngleSharp.Dom;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;

namespace KoeBook.Test.Epub;

public class ScrapingAozoraServiceTest
{
    private static readonly EpubDocument EmptySingleParagraph = new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph()] }] }] };

    public static object[][] ProcessChildrenTestCases()
    {
        // string: 読み込むhtml。これをclass = "main_text"なdivタグで囲ってテストに投げる
        // EpubDocument: ProcessChildren実行前のScrapingAozoraService._document。
        // CssClass[]: ProcessChildren実行前のScrapingAozoraService._document.CssClassesに追加したいCssClassを列挙する。
        // EpubDocument: ProcessChildren実行後にあるべき、ScrapingAozoraService._document。
        // CssClass[]: ProcessChildren実行後にあるべきScrapingAozoraService._document.CssClassesに追加したいCssClassを列挙する。 

        (string, EpubDocument, CssClass[], EpubDocument, CssClass[])[] patterns = [
            // レイアウト
            // 1.1 改丁
            (@"<span class=""notes"">［＃改丁］</span>", EmptySingleParagraph, [], new EpubDocument("", "", "", Guid.NewGuid()) {  Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph() { Text = "［＃改丁］", ScriptLine = new Core.Models.ScriptLine("", "", "") }] }] }] }, []),
        ];

        for (int i = 0; i < patterns.Length; i++)
        {
            patterns[i].Item2.CssClasses.AddRange(patterns[i].Item3);
            patterns[i].Item4.CssClasses.AddRange(patterns[i].Item5);
        }
        return patterns.Select(c => new object[] { ToMainText(c.Item1), c.Item2, c.Item4 }).ToArray();
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
    public async void ProcessChildrenTest(string html, EpubDocument initial, EpubDocument expected)
    {
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(request => request.Content(html));
        var mainText = doc.QuerySelector(".main_text");
        var scraper = new ScrapingAozoraService(new SplitBraceService(), new ScrapingClientService(new httpClientFactory(), TimeProvider.System));
        scraper._document() = initial;

        scraper.ProcessChildren(mainText!);

        var actual = scraper._document();
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Author, actual.Author);
        Assert.Equal(expected.CssClasses, actual.CssClasses);
        foreach ((var expectedChapter, var actualChapter) in expected.Chapters.Zip(actual.Chapters))
        {
            Assert.Equal(expectedChapter.Title, actualChapter.Title);
            foreach ((var expectedSection, var actualSection) in expectedChapter.Sections.Zip(actualChapter.Sections))
            {
                Assert.Equal(expectedSection.Title, actualSection.Title);
                foreach ((var expectedElement, var actualElement) in expectedSection.Elements.Zip(actualSection.Elements))
                {
                    switch (expectedElement,  actualElement)
                    {
                        case (Paragraph expectedParagraph, Paragraph actualParagraph):
                            Assert.Equal(expectedParagraph.ClassName, actualParagraph.ClassName);
                            Assert.Equal(expectedParagraph.Text, actualParagraph.Text);
                            Assert.NotNull(expectedParagraph.ScriptLine);
                            Assert.NotNull(actualParagraph.ScriptLine);
                            Assert.Equal(expectedParagraph.ScriptLine.Text, actualParagraph.ScriptLine.Text);
                            break;
                        case (Picture expectedPicture, Picture actualPicture):
                            Assert.Equal(expectedPicture.ClassName, actualPicture.ClassName);
                            Assert.Equal(expectedPicture.PictureFilePath, actualPicture.PictureFilePath);
                            break;
                        default:
                            Assert.Fail();
                            break;
                    }
                }
            }
        }
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
