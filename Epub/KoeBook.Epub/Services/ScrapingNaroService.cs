using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using KoeBook.Core;
using KoeBook.Core.Utilities;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KoeBook.Epub.Services
{
    public partial class ScrapingNaroService(IHttpClientFactory httpClientFactory, ISplitBraceService splitBraceService, [FromKeyedServices(nameof(ScrapingNaroService))] IScrapingClientService scrapingClientService) : IScrapingService
    {
        private readonly IHttpClientFactory _httpCliantFactory = httpClientFactory;
        private readonly ISplitBraceService _splitBraceService = splitBraceService;
        private readonly IScrapingClientService _scrapingClientService = scrapingClientService;
        private const string BaseUrl = "https://ncode.syosetu.com";

        public bool IsMatchSite(Uri uri)
        {
            return uri.Host == "ncode.syosetu.com";
        }

        public async ValueTask<EpubDocument> ScrapingAsync(string url, string coverFilePath, string imageDirectory, Guid id, CancellationToken ct)
        {
            var ncode = GetNcode(url);
            var novelInfo = await GetNovelInfoAsync(ncode, ct).ConfigureAwait(false);

            var config = Configuration.Default.WithDefaultLoader();
            using var context = BrowsingContext.New(config);

            IDocument doc;
            {
                // htmlはローカルでキャプチャする
                var html = await _scrapingClientService.GetAsStringAsync($"{BaseUrl}/{ncode}", ct).ConfigureAwait(false);
                doc = await context.OpenAsync(request => request.Content(html), ct).ConfigureAwait(false);
            }

            // title の取得
            var bookTitleElement = doc.QuerySelector(".novel_title")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, $"タイトルを取得できませんでした");
            var bookTitle = bookTitleElement.InnerHtml;

            // auther の取得
            var bookAutherElement = doc.QuerySelector(".novel_writername")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, $"著者を取得できませんでした");
            var bookAuther = bookAutherElement.QuerySelector("a") is IHtmlAnchorElement bookAutherTag
                ? bookAutherTag.InnerHtml
                : bookAutherElement.InnerHtml.Replace("作者：", "");

            var document = new EpubDocument(bookTitle, bookAuther, coverFilePath, id);
            if (novelInfo.IsSerial) // 連載の時
            {
                async IAsyncEnumerable<(string? title, Section section)> LoadDetailsAsync(IBrowsingContext context, NovelInfo novelInfo, string imageDirectory, [EnumeratorCancellation] CancellationToken ct)
                {
                    for (int i = 1; i <= novelInfo.GeneralAllNo; i++)
                    {
                        IDocument doc;
                        {
                            // htmlはローカルでキャプチャする
                            var html = await _scrapingClientService.GetAsStringAsync($"{BaseUrl}/{ncode}/{i}", ct).ConfigureAwait(false);
                            doc = await context.OpenAsync(request => request.Content(html), ct).ConfigureAwait(false);
                        }
                        yield return await ReadPageAsync(doc, true, imageDirectory, ct).ConfigureAwait(false);
                    }
                }

                string? chapterTitle = null;
                await foreach (var (title, section) in LoadDetailsAsync(context, novelInfo, imageDirectory, ct).ConfigureAwait(false).WithCancellation(ct))
                {
                    if (title != null)
                    {
                        if (title != chapterTitle)
                            document.Chapters.Add(new Chapter() { Title = title });
                    }
                    else
                        document.EnsureChapter();

                    document.Chapters[^1].Sections.Add(section);
                }
            }
            else // 短編の時
            {
                var (_, section) = await ReadPageAsync(doc, false, imageDirectory, ct).ConfigureAwait(false);

                document.Chapters.Add(new Chapter() { Title = null });
                document.Chapters[^1].Sections.Add(section);
            }
            return document;
        }

        private async ValueTask<(string? title, Section section)> ReadPageAsync(IDocument doc, bool isSerial, string imageDirectory, CancellationToken ct)
        {
            var lineBuilder = new SplittedLineBuilder();

            var chapterTitle = isSerial
                ? null
                : doc.QuerySelector(".chapter_title")?.InnerHtml;

            var sectionTitleElement = (isSerial
                ? doc.QuerySelector(".novel_subtitle")
                : doc.QuerySelector(".novel_title"))
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, "ページのタイトルが見つかりません");

            var sectionTitle = sectionTitleElement.InnerHtml;

            var section = new Section(sectionTitle);

            var main_text = doc.QuerySelector("#novel_honbun")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, "本文がありません");

            foreach (var item in main_text.Children)
            {
                if (item is not IHtmlParagraphElement)
                    throw new EbookException(ExceptionType.UnexpectedStructure);

                if (item.ChildElementCount == 0)
                {
                    if (!string.IsNullOrWhiteSpace(item.InnerHtml))
                        lineBuilder.Append(item.InnerHtml);
                }
                else if (item.Children is [var child])
                {
                    switch (child)
                    {
                        case { TagName: TagNames.Anchor, Children: [IHtmlImageElement img] } when img.Source is not null:
                            {
                                // 画像のダウンロード
                                var filePass = Path.Combine(imageDirectory, new Uri(img.Source, UriOptions.RawUri).Segments[^1].TrimEnd('/'));
                                using var fileSr = File.OpenWrite(filePass);
                                await _scrapingClientService.GetAsStreamAsync(img.Source, fileSr, ct).ConfigureAwait(false);
                                section.Elements.Add(new Picture(filePass));
                                break;
                            }
                        case { TagName: TagNames.Ruby }:
                            if (!string.IsNullOrWhiteSpace(item.InnerHtml))
                                lineBuilder.Append(item.InnerHtml);
                            break;
                        case { TagName: TagNames.BreakRow }:
                            foreach (var split in _splitBraceService.SplitBrace(lineBuilder.ToLinesAndClear()))
                            {
                                section.Elements.Add(new Paragraph() { Text = split });
                            }
                            break;
                        default:
                            throw new EbookException(ExceptionType.UnexpectedStructure);
                    }
                }
                else
                {
                    if (item.Children.Any(t => t.TagName != TagNames.Ruby))
                        throw new EbookException(ExceptionType.UnexpectedStructure);

                    if (!string.IsNullOrWhiteSpace(item.InnerHtml))
                        lineBuilder.Append(item.InnerHtml);
                }

                foreach (var split in _splitBraceService.SplitBrace(lineBuilder.ToLinesAndClear()))
                {
                    section.Elements.Add(new Paragraph() { Text = split });
                }
            }
            return (chapterTitle, section);
        }

        internal async ValueTask<NovelInfo> GetNovelInfoAsync(string ncode, CancellationToken ct)
        {
            // APIを利用して、noveltype : 連載(1)か短編(2)か、general_all_no : 全掲載部分数
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.syosetu.com/novelapi/api/?of=ga-nt-n&out=json&ncode={ncode}");
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");

            var client = _httpCliantFactory.CreateClient();
            var response = await client.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new EbookException(ExceptionType.HttpResponseError, $"URLが正しいかどうかやインターネットに正常に接続されているかどうかを確認してください。\nステータスコード: {response.StatusCode}");

            var result = response.Content.ReadFromJsonAsAsyncEnumerable<JsonElement>(ct);

            await using var enumerator = result.GetAsyncEnumerator(ct);

            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                throw new EbookException(ExceptionType.NarouApiFailed);
            var dataInfo = enumerator.Current.Deserialize<NaroResponseFirstElement>(JsonOptions.Web);
            if (dataInfo is not { Allcount: 1 })
                throw new EbookException(ExceptionType.NarouApiFailed);

            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
                throw new EbookException(ExceptionType.NarouApiFailed);

            var novelInfo = enumerator.Current.Deserialize<NovelInfo>(JsonOptions.Web);
            if (novelInfo is null || !novelInfo.Ncode.Equals(ncode, StringComparison.OrdinalIgnoreCase))
                throw new EbookException(ExceptionType.NarouApiFailed);

            if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                throw new EbookException(ExceptionType.NarouApiFailed);

            return novelInfo;
        }

        internal static string GetNcode(string url)
        {
            var uri = new Uri(url, UriOptions.RawUri);
            if (uri.GetLeftPart(UriPartial.Authority) != "https://ncode.syosetu.com")
                throw new EbookException(ExceptionType.InvalidUrl);

            return uri.Segments switch
            {
            // https://ncode.syosetu.com/n0000a/ のとき
            ["/", var ncode] when IsAscii(ncode) => ncode.TrimEnd('/'),
            // https://ncode.syosetu.com/n0000a/12 のとき
            ["/", var ncode, var num] when IsAscii(ncode) && num.TrimEnd('/').All(char.IsAsciiDigit) => ncode.TrimEnd('/'),
            // https://ncode.syosetu.com/novelview/infotop/ncode/n0000a/ のとき
            ["/", "novelview/", "infotop/", "ncode/", var ncode] when IsAscii(ncode) => ncode.TrimEnd('/'),
                _ => throw new EbookException(ExceptionType.InvalidUrl),
            };

            static bool IsAscii(string str)
                => str.All(char.IsAscii);
        }

        private record NaroResponseFirstElement(int Allcount);

        /// <summary>
        /// 小説情報
        /// </summary>
        /// <param name="Ncode">ncode</param>
        /// <param name="Noveltype">1: 連載, 2: 短編</param>
        /// <param name="GeneralAllNo">話数 (短編の場合は1)</param>
        internal record NovelInfo(
            [property: JsonRequired] string Ncode,
            [property: JsonRequired] int Noveltype,
            [property: JsonPropertyName("general_all_no"), JsonRequired] int GeneralAllNo)
        {
            /// <summary>
            /// 長編であるときtrue
            /// </summary>
            public bool IsSerial => Noveltype == 1;
        }
    }
}
