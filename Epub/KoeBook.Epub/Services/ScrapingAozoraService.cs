using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io;
using KoeBook.Core;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using Microsoft.Extensions.DependencyInjection;


namespace KoeBook.Epub.Services
{
    public partial class ScrapingAozoraService(ISplitBraceService splitBraceService, [FromKeyedServices(nameof(ScrapingAozoraService))] IScrapingClientService scrapingClientService) : IScrapingService
    {
        private readonly ISplitBraceService _splitBraceService = splitBraceService;
        private readonly IScrapingClientService _scrapingClientService = scrapingClientService;


        public bool IsMatchSite(Uri uri)
        {
            return uri.Host == "www.aozora.gr.jp";
        }

        public async ValueTask<EpubDocument> ScrapingAsync(string url, string coverFilePath, string imageDirectory, Guid id, CancellationToken ct)
        {
            var chapterNum = 0;
            var sectionNum = 0;
            var chapterExist = false;
            var sectionExist = false;

            var config = Configuration.Default.WithDefaultLoader();
            using var context = BrowsingContext.New(config);
            var doc = await context.OpenAsync(url, ct).ConfigureAwait(false);

            // title の取得
            var bookTitle = doc.QuerySelector(".title")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, $"タイトルの取得に失敗しました。\n以下のリンクから正しい小説のリンクを取得してください。\n{GetCardUrl(url)}");

            // auther の取得
            var bookAuther = doc.QuerySelector(".author")
                ?? throw new EbookException(ExceptionType.WebScrapingFailed, $"著者の取得に失敗しました。\n以下のリンクから正しい小説のリンクを取得してください。\n{GetCardUrl(url)}");

            // EpubDocument の生成
            var document = new EpubDocument(TextReplace(bookTitle.InnerHtml), TextReplace(bookAuther.InnerHtml), coverFilePath, id)
            {
                // EpubDocument.Chapters の生成
                Chapters = new List<Chapter>()
            };

            // 目次を取得
            var contents = doc.QuerySelectorAll(".midashi_anchor");

            // 目次からEpubDocumentを構成
            List<int> contentsIds = new List<int>() { 0 };
            // Chapter, Section が存在するとき、それぞれtrue
            chapterExist = false;
            sectionExist = false;
            if (contents.Length != 0)
            {
                int previousMidashiId = 0;
                foreach (var midashi in contents)
                {
                    if (midashi.Id != null)
                    {
                        var MidashiId = int.Parse(midashi.Id.Replace("midashi", ""));
                        if ((MidashiId - previousMidashiId) == 100)
                        {
                            document.Chapters.Add(new Chapter() { Title = TextProcess(midashi) });
                            chapterExist = true;
                        }
                        if ((MidashiId - previousMidashiId) == 10)
                        {
                            document.EnsureChapter();
                            document.Chapters[^1].Sections.Add(new Section(TextProcess(midashi)));
                            sectionExist = true;
                        }
                        contentsIds.Add(MidashiId);
                        previousMidashiId = MidashiId;
                    }
                }
            }
            else
            {
                document.Chapters.Add(new Chapter() { Title = null });
                document.Chapters[^1].Sections.Add(new Section(bookTitle.InnerHtml));
            }

            // 本文を取得
            var mainText = doc.QuerySelector(".main_text")!;


            // 本文を分割しながらEpubDocumntに格納
            // 直前のNodeを確認した操作で、その内容をParagraphに追加した場合、true
            bool previous = false;
            // 各ChapterとSection のインデックス
            chapterNum = -1;
            sectionNum = -1;

            // 直前のimgタグにaltがなかったときtrueになる。
            bool skipCaption = false;

            foreach (var element in mainText.Children)
            {
                var nextNode = element.NextSibling;
                if (element.TagName == "BR")
                {
                    if (previous == true)
                    {
                        document.EnsureSection(chapterNum);
                        document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                    }
                }
                else if (element.TagName == "DIV")
                {
                    var midashi = element.QuerySelector(".midashi_anchor");
                    if (midashi != null)
                    {
                        if (midashi.Id == null)
                            throw new EbookException(ExceptionType.WebScrapingFailed, "予期しないHTMLの構造です。\nclass=\"midashi_anchor\"ではなくid=\"midashi___\"が存在します。");

                        if (!int.TryParse(midashi.Id.Replace("midashi", ""), out var midashiId))
                            throw new EbookException(ExceptionType.WebScrapingFailed, $"予期しないアンカータグが見つかりました。id = {midashi.Id}");

                        if (contentsIds.Contains(midashiId))
                        {
                            var contentsId = contentsIds.IndexOf(midashiId);
                            switch (contentsIds[contentsId] - contentsIds[contentsId - 1])
                            {
                                case 100:
                                    if (chapterNum >= 0 && sectionNum >= 0)
                                    {
                                        document.Chapters[chapterNum].Sections[sectionNum].Elements.RemoveAt(document.Chapters[chapterNum].Sections[sectionNum].Elements.Count - 1);
                                    }
                                    chapterNum++;
                                    sectionNum = -1;
                                    break;
                                case 10:
                                    if (chapterNum == -1)
                                    {
                                        chapterNum++;
                                        sectionNum = -1;
                                    }
                                    if (chapterNum >= 0 && sectionNum >= 0)
                                    {
                                        document.Chapters[chapterNum].Sections[sectionNum].Elements.RemoveAt(document.Chapters[chapterNum].Sections[sectionNum].Elements.Count - 1);
                                    }
                                    sectionNum++;
                                    break;
                                default:
                                    break;
                            }
                        }
                        else //小見出し、行中小見出しの処理
                        {
                            if (chapterNum == -1)
                            {
                                if (chapterExist)
                                {
                                    document.Chapters.Insert(0, new Chapter());
                                }
                                chapterNum++;
                                sectionNum = -1;
                            }
                            if (sectionNum == -1)
                            {
                                if (sectionExist)
                                {
                                    document.EnsureChapter();
                                    document.Chapters[^1].Sections.Insert(0, new Section("___"));
                                }
                                sectionNum++;
                            }
                            document.EnsureParagraph(chapterNum, sectionNum);
                            if ((document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph))
                            {
                                paragraph.Text += TextProcess(midashi);
                                document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());

                                foreach (var splitText in _splitBraceService.SplitBrace(TextProcess(midashi)))
                                {
                                    if (document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph1)
                                    {
                                        paragraph1.Text += splitText;
                                    }
                                    document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                                }
                            }
                        }
                    }
                    else
                    {
                        if (element.ClassName == "caption")
                        {
                            // https://www.aozora.gr.jp/annotation/graphics.html#:~:text=%3Cdiv%20class%3D%22caption%22%3E を処理するための部分
                            document.EnsureParagraph(chapterNum, sectionNum);
                            var focusElements = document.Chapters[chapterNum].Sections[sectionNum].Elements;
                            if (focusElements[^1] is Paragraph paragraph)
                            {
                                var splitted = _splitBraceService.SplitBrace(TextProcess(element));
                                var first = true;

                                foreach (var text in splitted)
                                {
                                    if (first)
                                    {
                                        paragraph.Text += text;
                                        first = false;
                                    }
                                    else
                                        focusElements.Add(new Paragraph() { Text = text });
                                }
                            }
                        }
                        else
                        {
                            if (chapterNum == -1)
                            {
                                if (chapterExist)
                                {
                                    document.Chapters.Insert(0, new Chapter());
                                }
                                chapterNum++;
                                sectionNum = -1;
                            }
                            if (sectionNum == -1)
                            {
                                if (sectionExist)
                                {
                                    document.EnsureChapter();
                                    document.Chapters[^1].Sections.Insert(0, new Section("___"));
                                }
                                sectionNum++;
                            }
                            document.EnsureParagraph(chapterNum, sectionNum);
                            if ((document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph))
                            {
                                foreach (var splitText in _splitBraceService.SplitBrace(TextProcess(element)))
                                {
                                    if (document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph1)
                                    {
                                        paragraph1.Text += splitText;
                                    }
                                    document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                                }
                            }
                        }
                    }
                }
                else if (element.TagName == "IMG")
                {
                    if (element is IHtmlImageElement img)
                    {
                        if (chapterNum == -1)
                        {
                            if (chapterExist)
                            {
                                document.Chapters.Insert(0, new Chapter());
                            }
                            chapterNum++;
                            sectionNum = -1;
                        }
                        if (sectionNum == -1)
                        {
                            if (sectionExist)
                            {
                                document.EnsureChapter();
                                document.Chapters[^1].Sections.Insert(0, new Section("___"));
                            }
                            sectionNum++;
                        }

                        if (element.ClassName != "gaiji")
                        {
                            if (img.Source != null)
                            {
                                // 画像のダウンロード
                                var loader = context.GetService<IDocumentLoader>();
                                if (loader != null)
                                {
                                    var downloading = loader.FetchAsync(new DocumentRequest(new Url(img.Source)));
                                    ct.Register(() => downloading.Cancel());
                                    var response = await downloading.Task.ConfigureAwait(false);
                                    using var ms = new MemoryStream();
                                    await response.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
                                    var filePass = System.IO.Path.Combine(imageDirectory, FileUrlToFileName().Replace(img.Source, "$1"));
                                    File.WriteAllBytes(filePass, ms.ToArray());
                                    document.EnsureSection(chapterNum);
                                    if (document.Chapters[chapterNum].Sections[sectionNum].Elements.Count > 1)
                                    {
                                        document.Chapters[chapterNum].Sections[sectionNum].Elements.Insert(document.Chapters[chapterNum].Sections[sectionNum].Elements.Count - 1, new Picture(filePass));
                                    }
                                }
                            }
                            if (img.AlternativeText != null)
                            {
                                document.EnsureParagraph(chapterNum, sectionNum);
                                if ((document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph))
                                {
                                    paragraph.Text += TextReplace(img.AlternativeText);
                                    document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                                }
                                skipCaption = false;
                            }
                            else
                            {
                                skipCaption = true;
                            }
                        }
                    }
                }
                else if (element.TagName == "SPAN")
                {
                    if (element.ClassName == "caption")
                    {
                        if (skipCaption)
                        {
                            if ((document.Chapters[chapterNum].Sections[sectionNum].Elements[^2] is Paragraph paragraph))
                            {
                                paragraph.Text = TextProcess(element) + "の画像";
                            }
                        }
                        else
                        {
                            if ((document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph))
                            {
                                paragraph.Text = TextProcess(element) + "の画像";
                            }
                        }
                    }
                    else if (element.ClassName == "notes")
                    {
                        switch (element.InnerHtml)
                        {
                            case "［＃改丁］":
                                break;
                            case "［＃改ページ］":
                                break;
                            case "［＃改見開き］":
                                break;
                            case "［＃改段］":
                                break;
                            case "［＃ページの左右中央］":
                                break;
                            default:
                                document.EnsureParagraph(chapterNum, sectionNum);
                                if ((document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph))
                                {
                                    foreach (var splitText in _splitBraceService.SplitBrace(TextProcess(element)))
                                    {
                                        if (document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph1)
                                        {
                                            paragraph1.Text += splitText;
                                        }
                                        document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (chapterNum == -1)
                        {
                            if (chapterExist)
                            {
                                document.Chapters.Insert(0, new Chapter());
                            }
                            chapterNum++;
                            sectionNum = -1;
                        }
                        if (sectionNum == -1)
                        {
                            if (sectionExist)
                            {
                                document.EnsureChapter();
                                document.Chapters[^1].Sections.Insert(0, new Section("___"));
                            }
                            sectionNum++;
                        }

                        document.EnsureParagraph(chapterNum, sectionNum);
                        var focusElements = document.Chapters[chapterNum].Sections[sectionNum].Elements;
                        if (focusElements[^1] is Paragraph paragraph)
                        {
                            var splitted = _splitBraceService.SplitBrace(TextProcess(element));
                            var first = true;
                            foreach (var text in splitted)
                            {
                                if (first)
                                {
                                    paragraph.Text += text;
                                    first = false;
                                }
                                else
                                    focusElements.Add(new Paragraph { Text = text });
                            }
                        }
                        // 想定していない構造が見つかったことをログに出力した方が良い？
                    }
                }
                else
                {
                    if (chapterNum == -1)
                    {
                        if (chapterExist)
                        {
                            document.Chapters.Insert(0, new Chapter());
                        }
                        chapterNum++;
                        sectionNum = -1;
                    }
                    if (sectionNum == -1)
                    {
                        if (sectionExist)
                        {
                            document.EnsureChapter();
                            document.Chapters[^1].Sections.Insert(0, new Section("___"));
                        }
                        sectionNum++;
                    }
                    document.EnsureParagraph(chapterNum, sectionNum);
                    var focusElements = document.Chapters[chapterNum].Sections[sectionNum].Elements;
                    if (focusElements[^1] is Paragraph paragraph)
                    {
                        var splitted = _splitBraceService.SplitBrace(TextProcess(element));
                        var first = true;
                        foreach (var text in splitted)
                        {
                            if (first)
                            {
                                paragraph.Text += text;
                                first = false;
                            }
                            else
                                focusElements.Add(new Paragraph { Text = text });
                        }
                    }
                    // 想定していない構造が見つかったことをログに出力した方が良い？
                }

                if (nextNode != null)
                {
                    if (nextNode.NodeType == NodeType.Text)
                    {
                        if (!string.IsNullOrWhiteSpace(nextNode.Text()))
                        {
                            previous = true;

                            if (chapterNum == -1)
                            {
                                if (chapterExist)
                                {
                                    document.Chapters.Insert(0, new Chapter());
                                }
                                chapterNum++;
                                sectionNum = -1;
                            }
                            if (sectionNum == -1)
                            {
                                if (sectionExist)
                                {
                                    document.EnsureChapter();
                                    document.Chapters[^1].Sections.Insert(0, new Section("___"));
                                }
                                sectionNum++;
                            }
                            document.EnsureParagraph(chapterNum, sectionNum);
                            var focusElements = document.Chapters[chapterNum].Sections[sectionNum].Elements;
                            if (focusElements[^1] is Paragraph paragraph)
                            {
                                var splitted = _splitBraceService.SplitBrace(TextReplace(nextNode.Text()));
                                var first = true;
                                foreach (var text in splitted)
                                {
                                    if (first)
                                    {
                                        paragraph.Text += text;
                                        first = false;
                                    }
                                    else
                                        focusElements.Add(new Paragraph { Text = text });
                                }
                            }
                        }
                        else
                        {
                            previous = false;
                        }
                    }
                    else
                    {
                        previous = false;
                    }
                }
            }

            // 末尾の空のparagraphを削除
            document.Chapters[^1].Sections[^1].Elements.RemoveAt(document.Chapters[^1].Sections[^1].Elements.Count - 1);

            return document;
        }


        private static string TextProcess(IElement element)
        {
            string text = "";
            if (element.ChildElementCount == 0)
            {
                text += TextReplace(element.InnerHtml);
            }
            else
            {
                var rubies = element.QuerySelectorAll("ruby");
                if (rubies.Length > 0)
                {
                    if (element.Children[0].PreviousSibling is INode node)
                    {
                        if (node.NodeType == NodeType.Text)
                        {
                            if (!string.IsNullOrWhiteSpace(node.Text()))
                            {
                                text += TextReplace(node.Text());
                            }
                        }
                    }
                    foreach (var item in element.Children)
                    {
                        if (item.TagName == "RUBY")
                        {
                            if (item.QuerySelectorAll("img").Length > 0)
                            {
                                if (item.QuerySelector("rt") != null)
                                {
                                    text += TextReplace(item.QuerySelector("rt")!.TextContent);
                                }
                            }
                            else
                            {
                                text += TextReplace(item.OuterHtml);
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(item.TextContent) && (!string.IsNullOrEmpty(item.TextContent)))
                            {
                                text += TextReplace(item.TextContent);
                            }
                        }
                        if (item.NextSibling != null)
                        {
                            if (!string.IsNullOrWhiteSpace(item.NextSibling.TextContent) && (!string.IsNullOrEmpty(item.NextSibling.TextContent)))
                            {
                                text += TextReplace(item.NextSibling.Text());
                            }
                        }
                    }
                }
                else if (element.TagName == "RUBY")
                {
                    if (element.QuerySelectorAll("img").Length > 0)
                    {
                        if (element.QuerySelector("rt") != null)
                        {
                            text += TextReplace(element.QuerySelector("rt")!.TextContent);
                        }
                    }
                    else
                    {
                        text += TextReplace(element.OuterHtml);
                    }
                }
                else
                {
                    text += TextReplace(element.TextContent);
                }
            }
            return text;
        }


        // ローマ数字、改行の置換をまとめて行う。
        private static string TextReplace(string text)
        {
            string returnText = text;
            returnText = RomanNumImg().Replace(returnText, "$1");
            returnText = RomanNumText1().Replace(returnText, "$1");
            returnText = RomanNumText2().Replace(returnText, "$1");
            returnText = returnText.Replace("\n", "");
            return returnText;
        }


        private static string GetCardUrl(string url)
        {
            return UrlBookToCard().Replace(url, "$1card$2$3");
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"(https://www\.aozora\.gr\.jp/cards/\d{6}/)files/(\d{1,})_\d{1,}(\.html)")]
        private static partial System.Text.RegularExpressions.Regex UrlBookToCard();

        [System.Text.RegularExpressions.GeneratedRegex(@"<img src=""\.\./\.\./\.\./gaiji/1-1\d/1-1\d.{0,}\.png""alt=""※(ローマ数字(\d{1,})、1-1\d.{0,})"" class=""gaiji"">")]
        private static partial System.Text.RegularExpressions.Regex RomanNumImg();

        [System.Text.RegularExpressions.GeneratedRegex(@"※［＃ローマ数字(\d{1,})、1-1\d.{0,}］")]
        private static partial System.Text.RegularExpressions.Regex RomanNumText1();

        [System.Text.RegularExpressions.GeneratedRegex(@"※(ローマ数字(\d.{1,})、1-1\d.{0,})")]
        private static partial System.Text.RegularExpressions.Regex RomanNumText2();

        [System.Text.RegularExpressions.GeneratedRegex(@"http.{1,}/([^/]{0,}\.[^/]{1,})")]
        private static partial System.Text.RegularExpressions.Regex FileUrlToFileName();
    }
}
