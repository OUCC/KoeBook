using System.Text;
using AngleSharp;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;
using System.Runtime.CompilerServices;
using System.Linq;

namespace KoeBook.Test.Epub
{
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
        [MemberData(nameof(TestCases))]
        public async void ProcessChildrenTest(string html, EpubDocument initial, EpubDocument expexted)
        {
            var config = Configuration.Default.WithDefaultLoader();
            using var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(request => request.Content(html));
            var mainText = doc.QuerySelector(".main_text");
            var scraper = new ScrapingAozoraService(new SplitBraceService(), new ScrapingClientService(new httpClientFactory(), TimeProvider.System));
            scraper._document() = initial;

            scraper.ProcessChildren(mainText, [""], "");

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
    }

    internal class httpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return httpClient;
        }

        private static readonly HttpClient httpClient = new HttpClient();

    }
}
file static class Proxy
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_document")]
    public static extern ref EpubDocument _document(this ScrapingAozoraService scraper);

    
}
