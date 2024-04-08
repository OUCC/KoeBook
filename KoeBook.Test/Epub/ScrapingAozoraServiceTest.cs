using System.Runtime.CompilerServices;
using AngleSharp;
using AngleSharp.Dom;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;
using System.Runtime.CompilerServices;
using System.Linq;

namespace KoeBook.Test.Epub;

public class ScrapingAozoraServiceTest
{
        private static readonly EpubDocument EmptySingleParagraph = new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph()] }] }] };

        public static object[][] TestCases()
        {
            (string, EpubDocument, EpubDocument)[] cases = [
                // レイアウト
                // 1.1 改丁
                (ToMainText(@"<span class=""notes"">［＃改丁］</span>"), EmptySingleParagraph , new EpubDocument("", "", "", Guid.NewGuid()) { Chapters = [new Chapter() { Sections = [new Section("") { Elements = [new Paragraph() { Text = "［＃改丁］", ScriptLine = new Core.Models.ScriptLine("", "", "") }] }] }] }),
            ];
            return cases.Select(c => new object[] { c.Item1, c.Item2 }).ToArray();
        }

        /// <summary>
        /// を"<div class = \"main_text\"></div>"で囲む
        /// </summary>
        /// <param name="text">divタグで囲むhtmlの要素</param>
        /// <returns>divタグで囲まれた<paramref name="text"/></returns>
        private static string ToMainText(string text)
        {
            var builder = new StringBuilder();
            builder.Append(@"<div class = ""main_text"">");
            builder.Append(text);
            builder.Append("</div>");
            return builder.ToString();
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
}
