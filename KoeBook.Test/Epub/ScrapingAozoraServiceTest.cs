using System.Runtime.CompilerServices;
using AngleSharp;
using AngleSharp.Dom;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;
using KoeBook.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using KoeBook.Epub.Contracts.Services;

namespace KoeBook.Test.Epub;

public class ScrapingAozoraServiceTest : DiTestBase
{
    private readonly ScrapingAozoraService _scrapingAozoraService;

    public ScrapingAozoraServiceTest()
    {
        _scrapingAozoraService = Host.Services
            .GetServices<IScrapingService>()
            .OfType<ScrapingAozoraService>()
            .Single();
    }

    private static EpubDocument EmptySingleParagraph
    {
        get { return new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph()] }] }] }; }
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
    // レイアウト1.1 改丁
    [InlineData(@"<div><span class=""notes"">［＃改丁］</span><br></div>", "［＃改丁］", "")]
    // レイアウト1.2 改ページ
    [InlineData(@"<div><span class=""notes"">［＃改ページ］</span><br></div>", "［＃改ページ］", "")]
    // レイアウト1.3 改見開き
    [InlineData(@"<div><span class=""notes"">［＃改見開き］</span><br></div>", "［＃改見開き］", "")]
    // レイアウト1.4 改段
    [InlineData(@"<div><span class=""notes"">［＃改段］</span><br /><div>", "［＃改段］", "")]
    public async void ProcessChildrenlayout1Test(string html, string expectedPragraphText, string expectedScriptText)
    {
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(request => request.Content(html));
        var mainText = doc.DocumentElement.LastElementChild?.LastElementChild;
        if (mainText == null)
            Assert.Fail();
        var document = EmptySingleParagraph;

        _scrapingAozoraService.ProcessChildren(document, mainText, "");

        var chapter = Assert.Single(document.Chapters);
        var section = Assert.Single(chapter.Sections);
        var paragraph = Assert.IsType<Paragraph>(section.Elements[^1]);
        Assert.Equal(expectedPragraphText, paragraph.Text);
        Assert.Equal(string.Empty, paragraph.ClassName);
        Assert.NotNull(paragraph.ScriptLine);
        Assert.Equal(expectedScriptText, paragraph.ScriptLine.Text);
    }

    // Classes の各 value は、対応するclass で、ソースに出てきたものの内、最大のものの値をほじするようにする。
    public static object[][] ProcessChildrenlayout2TestCases()
    {
        (string, Paragraph[], (string, (int, int))[])[] cases = [
            // レイアウト2.1 1行だけの字下げ
            (@"<div class=""jisage_3"" style=""margin-left: 3em"">text<br /></div><br>", [new Paragraph() { Text = "text", ClassName = "jisage_3", ScriptLine = new ScriptLine("text", "", "") }], [("jisage", (1, 3))]),
            // レイアウト2.2 ブロックでの字下げ
            (@"<div class=""jisage_3"" style=""margin-left: 3em"">text1<br />text2<br /></div><br>", [new Paragraph() { Text = "text1", ClassName = "jisage_3", ScriptLine = new ScriptLine("text1", "", "") }, new Paragraph() { Text = "text2", ClassName = "jisage_3", ScriptLine = new ScriptLine("text2", "", "") },], [("jisage", (1, 3))]),
            // レイアウト2.3 凹凸の複雑な字下げ
            (@"<div class=""burasage"" style=""margin-left: 3em; text_indent: -1em;"">Long Text</div>", [new Paragraph() { Text = "Long Text", ClassName = "jisage_3 text_indent_-1" }], [("jisage", (1, 3)), ("text_indent", (-1, 0))]),
            // レイアウト2.4 は特定の書き方について述べていないので省略。
            // レイアウト2.5 地付き
            (@"<div class=""chitsuki_0"" style=""text-align:right; margin-right: 0em"">text</div>", [new Paragraph() { Text = "text", ClassName = "chitsuki_0", ScriptLine = new ScriptLine("text", "", "") }], [("chitsuki", (0, 0))]),


            // </div>の後の<br />がないパターン
            (@"<div class=""jisage_3"" style=""margin-left: 3em"">text<br /></div>", [new Paragraph() { Text = "text", ClassName = "jisage_3", ScriptLine = new ScriptLine("text", "", "") }], [("jisage", (1, 3))]),
            // </div>の前の<br />がないパターン
            (@"<div class=""burasage"" style=""margin-left: 1em; text_indent: -1em;"">text</div>", [new Paragraph() { Text = "text", ClassName = "jisage_3 text_indent_-1", ScriptLine = new ScriptLine("text", "", "") }], [("jisage", (1, 3)), ("text_indent", (-1, 0))]),

        ];
        return cases.Select(c => new object[] { ToMainText(c.Item1), c.Item2, c.Item3 }).ToArray();
    }

    [Theory]
    [MemberData(nameof(ProcessChildrenlayout2TestCases))]
    public async void ProcessChildrenlayout2Test(string html, IReadOnlyCollection<Paragraph> expectedParagraphs, IEnumerable<(string key, (int min, int max) value)> expectedDictionary)
    {
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(request => request.Content(html));
        var mainText = doc.QuerySelector(".main_text");
        if (mainText == null)
            Assert.Fail();
        var document = EmptySingleParagraph;
        _scrapingAozoraService._Classes().Clear();

        _scrapingAozoraService.ProcessChildren(document, mainText, "");

        var chapter = Assert.Single(document.Chapters);
        var section = Assert.Single(chapter.Sections);
        Assert.Equal(expectedParagraphs.Count, document.Chapters[^1].Sections[^1].Elements.Count);
        Assert.All(expectedParagraphs.Zip(document.Chapters[^1].Sections[^1].Elements), v =>
        {
            var actualParagraph = Assert.IsType<Paragraph>(v.Second);
            Assert.Equal(v.First.Text, actualParagraph.Text);
            Assert.Equal(v.First.ClassName, actualParagraph.ClassName);
            Assert.NotNull(actualParagraph.ScriptLine);
            Assert.Equal(v.First.ScriptLine?.Text, actualParagraph.ScriptLine.Text);
        });
        // ScrapingAozoraService.Classes の確認
        Assert.All(expectedDictionary, expectedKeyValuePair =>
        {
            Assert.True(_scrapingAozoraService._Classes().TryGetValue(expectedKeyValuePair.key, out var actualValue));
            Assert.True(actualValue.min <= expectedKeyValuePair.value.min);
            Assert.True(actualValue.max >= expectedKeyValuePair.value.max);
        });
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

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Classes")]
    public static extern Dictionary<string, (int min, int max)> _Classes(this ScrapingAozoraService scraper);
}
