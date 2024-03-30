﻿using System.Net.Http.Json;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
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

        public bool IsMatchSite(Uri uri)
        {
            return uri.Host == "ncode.syosetu.com";
        }

        public async ValueTask<EpubDocument> ScrapingAsync(string url, string coverFilePath, string imageDirectory, Guid id, CancellationToken ct)
        {
            var config = Configuration.Default.WithDefaultLoader();
            using var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(url, ct).ConfigureAwait(false);

            // title の取得
            var bookTitleElement = doc.QuerySelector(".novel_title")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, $"タイトルを取得できませんでした");
            var bookTitle = bookTitleElement.InnerHtml;

            // auther の取得
            var bookAutherElement = doc.QuerySelector(".novel_writername")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, $"著者を取得できませんでした");
            var bookAuther = string.Empty;
            if (bookAutherElement.QuerySelector("a") is IHtmlAnchorElement bookAutherAnchorElement)
            {
                bookAuther = bookAutherAnchorElement.InnerHtml;
            }
            else
            {
                bookAuther = bookAutherElement.InnerHtml.Replace("作者：", "");
            }

            bool isRensai = true;
            int allNum = 0;

            var apiUrl = $"https://api.syosetu.com/novelapi/api/?of=ga-nt&ncode={UrlToNcode().Replace(url, "$1")}&out=json";

            // APIを利用して、noveltype : 連載(1)か短編(2)か、general_all_no : 全掲載部分数
            var message = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, apiUrl);
            message.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/96.0.4664.45 Safari/537.36");
            var client = _httpCliantFactory.CreateClient();
            var result = await client.SendAsync(message, ct).ConfigureAwait(false);
            var test = await result.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!result.IsSuccessStatusCode)
                throw new EbookException(ExceptionType.HttpResponseError, $"URLが正しいかどうかやインターネットに正常に接続されているかどうかを確認してください。\nステータスコード: {result.StatusCode}\nメッセージ：{test}");

            var content = await result.Content.ReadFromJsonAsync<BookInfo[]>(ct).ConfigureAwait(false);
            if (content != null)
            {
                if (content[1].noveltype == null)
                    throw new EbookException(ExceptionType.NarouApiFailed);

                if (content[1].noveltype == 2)
                {
                    isRensai = false;
                }

                if (content[1].general_all_no != null)
                {
                    allNum = (int)content[1].general_all_no!;
                }

                if (allNum == 0)
                    throw new EbookException(ExceptionType.NarouApiFailed);
            }

            var document = new EpubDocument(bookTitle, bookAuther, coverFilePath, id);
            if (isRensai) // 連載の時
            {
                List<SectionWithChapterTitle> SectionWithChapterTitleList = new List<SectionWithChapterTitle>();
                for (int i = 1; i <= allNum; i++)
                {
                    await Task.Delay(1500, ct);
                    var pageUrl = Path.Combine(url, i.ToString());
                    var load = await ReadPageAsync(pageUrl, isRensai, imageDirectory, ct).ConfigureAwait(false);
                    SectionWithChapterTitleList.Add(load);
                }
                string? chapterTitle = null;
                foreach (var sectionWithChapterTitle in SectionWithChapterTitleList)
                {
                    if (sectionWithChapterTitle == null)
                        throw new EbookException(ExceptionType.WebScrapingFailed, "ページの取得に失敗しました");

                    if (sectionWithChapterTitle.title != null)
                    {
                        if (sectionWithChapterTitle.title != chapterTitle)
                        {
                            chapterTitle = sectionWithChapterTitle.title;
                            document.Chapters.Add(new Chapter() { Title = chapterTitle });
                            document.Chapters[^1].Sections.Add(sectionWithChapterTitle.section);
                        }
                        else
                        {
                            document.Chapters[^1].Sections.Add(sectionWithChapterTitle.section);
                        }
                    }
                    else
                    {
                        if (document.Chapters.Count == 0)
                        {
                            document.Chapters.Add(new Chapter());
                        }
                        document.Chapters[^1].Sections.Add(sectionWithChapterTitle.section);
                    }
                }
            }
            else // 短編の時
            {
                var load = await ReadPageAsync(url, isRensai, imageDirectory, ct).ConfigureAwait(false);
                if (load != null)
                {
                    document.Chapters.Add(new Chapter() { Title = null });
                    document.Chapters[^1].Sections.Add(load.section);
                }
            }
            return document;
        }

        public record BookInfo(int? allcount, int? noveltype, int? general_all_no);

        private record SectionWithChapterTitle(string? title, Section section);

        private async ValueTask<SectionWithChapterTitle> ReadPageAsync(string url, bool isRensai, string imageDirectory, CancellationToken ct)
        {
            var lineBuilder = new SplittedLineBuilder();

            var config = Configuration.Default.WithDefaultLoader();
            using var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(url, ct).ConfigureAwait(false);

            string? chapterTitle = null;
            if (!isRensai)
            {
                var chapterTitleElement = doc.QuerySelector(".chapter_title");
                if (chapterTitleElement != null)
                {
                    if (chapterTitleElement.InnerHtml != null)
                    {
                        chapterTitle = chapterTitleElement.InnerHtml;
                    }
                }
            }

            IElement? sectionTitleElement = null;
            if (isRensai)
            {
                sectionTitleElement = doc.QuerySelector(".novel_subtitle");
            }
            else
            {
                sectionTitleElement = doc.QuerySelector(".novel_title");
            }

            if (sectionTitleElement == null)
                throw new EbookException(ExceptionType.WebScrapingFailed, "ページのタイトルが見つかりません");

            var sectionTitle = sectionTitleElement.InnerHtml;

            var section = new Section(sectionTitleElement.InnerHtml);

            var main_text = doc.QuerySelector("#novel_honbun")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, "本文がありません");

            foreach (var item in main_text.Children)
            {
                if (item is not IHtmlParagraphElement)
                    throw new EbookException(ExceptionType.UnexpectedStructure);

                if (item.ChildElementCount == 0)
                {
                    if (!string.IsNullOrWhiteSpace(item.InnerHtml))
                    {
                        lineBuilder.Append(item.InnerHtml);
                    }
                }
                else if (item.ChildElementCount == 1)
                {
                    if (item.Children[0] is IHtmlAnchorElement aElement)
                    {
                        if (aElement.ChildElementCount != 1)
                            throw new EbookException(ExceptionType.UnexpectedStructure);

                        if (aElement.Children[0] is IHtmlImageElement img)
                        {
                            if (img.Source == null)
                                throw new EbookException(ExceptionType.UnexpectedStructure);

                            // 画像のダウンロード
                            var loader = context.GetService<IDocumentLoader>();
                            if (loader != null)
                            {
                                var downloading = loader.FetchAsync(new DocumentRequest(new Url(img.Source)));
                                ct.Register(() => downloading.Cancel());
                                var response = await downloading.Task.ConfigureAwait(false);
                                using var ms = new MemoryStream();
                                await response.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
                                var filePass = Path.Combine(imageDirectory, FileUrlToFileName().Replace(response.Address.Href, "$1"));
                                File.WriteAllBytes(filePass, ms.ToArray());
                                section.Elements.Add(new Picture(filePass));
                            }
                        }
                    }
                    else if (item.Children[0].TagName == "RUBY")
                    {
                        if (!string.IsNullOrWhiteSpace(item.InnerHtml))
                        {
                            lineBuilder.Append(item.InnerHtml);
                        }
                    }
                    else if (item.Children[0] is IHtmlBreakRowElement)
                    {
                        foreach (var split in _splitBraceService.SplitBrace(lineBuilder.ToLinesAndClear()))
                        {
                            section.Elements.Add(new Paragraph() { Text = split });
                        }
                    }
                    else
                        throw new EbookException(ExceptionType.UnexpectedStructure);
                }
                else
                {
                    bool isAllRuby = true;
                    foreach (var tags in item.Children)
                    {
                        if (tags.TagName != "RUBY")
                        {
                            isAllRuby = false;
                            break;
                        }
                    }

                    if (!isAllRuby)
                        throw new EbookException(ExceptionType.UnexpectedStructure);

                    if (!string.IsNullOrWhiteSpace(item.InnerHtml))
                    {
                        lineBuilder.Append(item.InnerHtml);
                    }
                }
                foreach (var split in _splitBraceService.SplitBrace(lineBuilder.ToLinesAndClear()))
                {
                    section.Elements.Add(new Paragraph() { Text = split });
                }
            }
            return new SectionWithChapterTitle(chapterTitle, section);
        }


        [System.Text.RegularExpressions.GeneratedRegex(@"https://.{5,7}.syosetu.com/(.{7}).?")]
        private static partial System.Text.RegularExpressions.Regex UrlToNcode();

        [System.Text.RegularExpressions.GeneratedRegex(@"http.{1,}/([^/]{0,}\.[^/]{1,})")]
        private static partial System.Text.RegularExpressions.Regex FileUrlToFileName();
    }
}
