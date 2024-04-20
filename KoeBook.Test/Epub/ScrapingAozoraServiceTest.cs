using System.Runtime.CompilerServices;
using AngleSharp;
using AngleSharp.Dom;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;
using KoeBook.Core.Models;

namespace KoeBook.Test.Epub;

public class ScrapingAozoraServiceTest
{
    private static readonly EpubDocument EmptySingleParagraph = new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph()] }] }] };

    /// <summary>
    /// (htmlの要素の)テキストを"<div class = \"main_text\"></div>"で囲む
    /// </summary>
    /// <param name="text">divタグで囲むhtmlの要素</param>
    /// <returns>divタグで囲まれた<paramref name="text"/></returns>
    private static string ToMainText(string text)
    {
        return @$"<div class = ""main_text"">{text}</div>";
    }

    public static object[][] ProcessChildrenlayout1TestCases()
    {
        (string, Paragraph)[] cases = [
            // レイアウト1.1 改丁
            (@"<span class=""notes"">［＃改丁］</span><br>", new Paragraph() { Text = "［＃改丁］", ScriptLine = new ScriptLine("", "", "") }),
            // レイアウト1.2 改ページ
            (@"<span class=""notes"">［＃改ページ］</span><br>", new Paragraph() { Text = "［＃改ページ］", ScriptLine = new ScriptLine("", "", "") }),
            // レイアウト1.3 改見開き
            (@"<span class=""notes"">［＃改見開き］</span><br>", new Paragraph() { Text = "［＃改見開き］", ScriptLine = new ScriptLine("", "", "") }),
            // レイアウト1.4 改段
            (@"<span class=""notes"">［＃改段］</span><br />", new Paragraph() { Text = "［＃改段］", ScriptLine = new ScriptLine("", "", "") }),
        ];
        return cases.Select(c => new object[] { ToMainText(c.Item1), c.Item2 }).ToArray();
    }

    [Theory]
    [MemberData(nameof(ProcessChildrenlayout1TestCases))]
    public async void ProcessChildrenlayout1Test(string html, Paragraph expected)
    {
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(request => request.Content(html));
        var mainText = doc.QuerySelector(".main_text");
        if (mainText == null)
            Assert.Fail();
        var scraper = new ScrapingAozoraService(new SplitBraceService(), new ScrapingClientService(new httpClientFactory(), TimeProvider.System));
        var document = EmptySingleParagraph;

        scraper.ProcessChildren(document, mainText, "");

        Assert.Single(document.Chapters);
        Assert.Single(document.Chapters[^1].Sections);
        Assert.Single(document.Chapters[^1].Sections);
        Assert.IsType<Paragraph>(document.Chapters[^1].Sections[^1].Elements[^1]);
        if (document.Chapters[^1].Sections[^1].Elements[^1] is Paragraph paragraph)
        {
            Assert.Equal(expected.Text, paragraph.Text);
            Assert.Equal(expected.ClassName, paragraph.ClassName);
            Assert.NotNull(paragraph.ScriptLine);
            Assert.Equal(expected.ScriptLine?.Text, paragraph.ScriptLine.Text);
        }
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
    public async void ProcessChildrenlayout2Test(string html, IReadOnlyCollection<Paragraph> expectedParagraphs, IEnumerable<(string, (int min, int max))> expectedDictionary)
    {
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        var doc = await context.OpenAsync(request => request.Content(html));
        var mainText = doc.QuerySelector(".main_text");
        if (mainText == null)
            Assert.Fail();
        var scraper = new ScrapingAozoraService(new SplitBraceService(), new ScrapingClientService(new httpClientFactory(), TimeProvider.System));
        var document = EmptySingleParagraph;

        scraper.ProcessChildren(document, mainText, "");

        Assert.Single(document.Chapters);
        Assert.Single(document.Chapters[^1].Sections);
        Assert.Equal(expectedParagraphs.Count, document.Chapters[^1].Sections[^1].Elements.Count);
        foreach ((var expectedParagraph, var actualElement) in expectedParagraphs.Zip(document.Chapters[^1].Sections[^1].Elements))
        {
            Assert.IsType<Paragraph>(actualElement);
            if (actualElement is Paragraph actualParagraph)
            {
                Assert.Equal(expectedParagraph.Text, actualParagraph.Text);
                Assert.Equal(expectedParagraph.ClassName, actualParagraph.ClassName);
                Assert.NotNull(actualParagraph.ScriptLine);
                Assert.Equal(expectedParagraph.ScriptLine?.Text, actualParagraph.ScriptLine.Text);
            }
            // ScrapingAozoraService.Classes の確認
            foreach ((var key, var exceptedValue) in expectedDictionary)
            {
                Assert.True(scraper._Classes().ContainsKey(key));
                Assert.True(scraper._Classes()[key].min <= exceptedValue.min);
                Assert.True(scraper._Classes()[key].max >= exceptedValue.max);
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

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Classes")]
    public static extern Dictionary<string, (int min, int max)> _Classes(this ScrapingAozoraService scraper);
}
