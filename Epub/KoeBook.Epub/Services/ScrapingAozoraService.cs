using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;
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
            var document = new EpubDocument(TextReplace(bookTitle.InnerHtml), TextReplace(bookAuther.InnerHtml), coverFilePath, id);

            var (contentsIds, hasChapter, hasSection) = LoadToc(doc, document);

            // 本文を取得
            var mainText = doc.QuerySelector(".main_text")!;


            // 本文を分割しながらEpubDocumntに格納
            // 直前のNodeを確認した操作で、その内容をParagraphに追加した場合、true
            var previous = false;
            // 各ChapterとSection のインデックス
            var chapterNum = -1;
            var sectionNum = -1;

            // 直前のimgタグにaltがなかったときtrueになる。
            var skipCaption = false;

            foreach (var element in mainText.Children)
            {
                var nextNode = element.NextSibling;
                switch (element.TagName)
                {
                    case TagNames.A:
                        if (previous)
                        {
                            document.EnsureSection(chapterNum);
                            document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                        }
                        break;
                    case TagNames.Div:
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
                                            document.Chapters[chapterNum].Sections[sectionNum].Elements.RemoveAt(^1);
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
                                            document.Chapters[chapterNum].Sections[sectionNum].Elements.RemoveAt(^1);
                                        }
                                        sectionNum++;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else //小見出し、行中小見出しの処理
                            {
                                (chapterNum, sectionNum) = SetChapterAndSection(document, hasChapter, hasSection, chapterNum, sectionNum);
                                document.EnsureParagraph(chapterNum, sectionNum);
                                AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, element, true);
                            }
                        }
                        else
                        {
                            if (element.ClassName == "caption")
                            {
                                // https://www.aozora.gr.jp/annotation/graphics.html#:~:text=%3Cdiv%20class%3D%22caption%22%3E を処理するための部分
                                document.EnsureParagraph(chapterNum, sectionNum);
                                AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, element, false);
                            }
                            else
                            {
                                (chapterNum, sectionNum) = SetChapterAndSection(document, hasChapter, hasSection, chapterNum, sectionNum);
                                document.EnsureParagraph(chapterNum, sectionNum);
                                AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, element, true);
                            }
                        }

                        break;
                    case TagNames.Img:
                        {
                            var img = (IHtmlImageElement)element;

                            (chapterNum, sectionNum) = SetChapterAndSection(document, hasChapter, hasSection, chapterNum, sectionNum);

                            if (element.ClassName == "gaiji")
                                break;

                            if (img.Source != null)
                            {
                                // 画像のダウンロード 
                                var filePass = Path.Combine(imageDirectory, FileUrlToFileName().Replace(img.Source, "$1"));
                                await _scrapingClientService.DownloadToFileAsync(img.Source, filePass, ct).ConfigureAwait(false);
                                document.EnsureSection(chapterNum);
                                if (document.Chapters[chapterNum].Sections[sectionNum].Elements.Count > 1)
                                {
                                    document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Picture(filePass));
                                }
                            }

                            if (img.AlternativeText is null)
                            {
                                skipCaption = true;
                                continue;
                            }

                            document.EnsureParagraph(chapterNum, sectionNum);
                            if (document.Chapters[chapterNum].Sections[sectionNum].Elements[^1] is Paragraph paragraph)
                            {
                                paragraph.Text += TextReplace(img.AlternativeText);
                                document.Chapters[chapterNum].Sections[sectionNum].Elements.Add(new Paragraph());
                            }
                            skipCaption = false;
                            break;
                        }
                    case TagNames.Span:
                        if (element.ClassName == "caption")
                        {
                            if (document.Chapters[chapterNum].Sections[sectionNum].Elements[skipCaption ? ^2 : ^1] is Paragraph paragraph)
                                paragraph.Text = TextProcess(element) + "の画像";
                        }
                        else if (element.ClassName == "notes")
                        {
                            switch (element.InnerHtml)
                            {
                                case "［＃改丁］":
                                case "［＃改ページ］":
                                case "［＃改見開き］":
                                case "［＃改段］":
                                case "［＃ページの左右中央］":
                                    break;
                                default:
                                    document.EnsureParagraph(chapterNum, sectionNum);
                                    AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, element, true);
                                    break;
                            }
                        }
                        else
                        {
                            (chapterNum, sectionNum) = SetChapterAndSection(document, hasChapter, hasSection, chapterNum, sectionNum);
                            document.EnsureParagraph(chapterNum, sectionNum);
                            AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, element, false);
                            // 想定していない構造が見つかったことをログに出力した方が良い？
                        }

                        break;
                    default:
                        (chapterNum, sectionNum) = SetChapterAndSection(document, hasChapter, hasSection, chapterNum, sectionNum);
                        document.EnsureParagraph(chapterNum, sectionNum);
                        AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, element, false);
                        break;
                        // 想定していない構造が見つかったことをログに出力した方が良い？
                }

                if (nextNode is null)
                    continue;

                if (nextNode.NodeType != NodeType.Text || string.IsNullOrWhiteSpace(nextNode.TextContent))
                {
                    previous = false;
                    continue;
                }

                previous = true;

                (chapterNum, sectionNum) = SetChapterAndSection(document, hasChapter, hasSection, chapterNum, sectionNum);
                document.EnsureParagraph(chapterNum, sectionNum);
                AddParagraphs(document.Chapters[chapterNum].Sections[sectionNum].Elements, nextNode.TextContent, false);
            }

            // 末尾の空のparagraphを削除
            document.Chapters[^1].Sections[^1].Elements.RemoveAt(^1);

            return document;
        }

        private static string TextProcess(IElement element)
        {
            if (element.ChildElementCount == 0)
            {
                return TextReplace(element.InnerHtml);
            }
            else
            {
                var rubies = element.QuerySelectorAll(TagNames.Ruby);
                if (rubies.Length > 0)
                {
                    var resultBuilder = new StringBuilder();
                    if (element.Children[0].PreviousSibling is INode node)
                    {
                        if (node.NodeType == NodeType.Text)
                        {
                            if (!string.IsNullOrWhiteSpace(node.Text()))
                            {
                                resultBuilder.Append(TextReplace(node.Text()));
                            }
                        }
                    }

                    foreach (var item in element.Children)
                    {
                        if (item.TagName == TagNames.Ruby)
                        {
                            if (item.QuerySelectorAll("img").Length > 0)
                            {
                                if (item.QuerySelector("rt") is { TextContent: var text })
                                {
                                    resultBuilder.Append(TextReplace(text));
                                }
                            }
                            else
                            {
                                resultBuilder.Append(TextReplace(item.OuterHtml));
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(item.TextContent) && (!string.IsNullOrEmpty(item.TextContent)))
                            {
                                resultBuilder.Append(TextReplace(item.TextContent));
                            }
                        }
                        if (item.NextSibling != null)
                        {
                            if (!string.IsNullOrWhiteSpace(item.NextSibling.TextContent) && (!string.IsNullOrEmpty(item.NextSibling.TextContent)))
                            {
                                resultBuilder.Append(TextReplace(item.NextSibling.Text()));
                            }
                        }
                    }
                    return resultBuilder.ToString();
                }
                else if (element.TagName == TagNames.Ruby)
                {
                    if (element.QuerySelectorAll("img").Length > 0)
                    {
                        if (element.QuerySelector("rt") is { TextContent: var text })
                            return TextReplace(text);
                        else
                            return "";
                    }
                    else
                    {
                        return TextReplace(element.OuterHtml);
                    }
                }
                else
                {
                    return TextReplace(element.TextContent);
                }
            }
        }

        private void AddParagraphs(List<Models.Element> focusElements, IElement element, bool lastEmpty)
        {
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

                if (lastEmpty)
                    focusElements.Add(new Paragraph());
            }
        }

        private void AddParagraphs(List<Models.Element> focusElements, string input, bool lastEmpty)
        {
            if (focusElements[^1] is Paragraph paragraph)
            {
                var splitted = _splitBraceService.SplitBrace(TextReplace(input));
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

                if (lastEmpty)
                    focusElements.Add(new Paragraph());
            }
        }

        /// <summary>
        /// ローマ数字、改行の置換をまとめて行う。
        /// </summary>
        private static string TextReplace(string text)
        {
            string returnText = text;
            returnText = RomanNumImg().Replace(returnText, "$1");
            returnText = RomanNumText1().Replace(returnText, "$1");
            returnText = RomanNumText2().Replace(returnText, "$1");
            returnText = returnText.Replace("\n", "");
            return returnText;
        }

        /// <summary>
        /// 目次からEpubDocuemntを構成します
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item>contentsIds: 見出しIDの数字部分。※EpubDocumentのChapter, Sectionとは一致しません</item>
        /// <item>Chapterが存在するとき</item>
        /// <item>Sectionが存在するとき</item>
        /// </list>
        /// </returns>
        private static (List<int> contentsIds, bool hasChapter, bool hasSection) LoadToc(IDocument doc, EpubDocument epubDocument)
        {
            // 目次を取得
            var contents = doc.QuerySelectorAll(".midashi_anchor");

            // 目次からEpubDocumentを構成
            var contentsIds = new List<int>() { 0 };
            // Chapter, Section が存在するとき、それぞれtrue
            var hasChapter = false;
            var hasSection = false;
            if (contents.Length != 0)
            {
                int previousMidashiId = 0;
                foreach (var midashi in contents)
                {
                    if (midashi.Id != null)
                    {
                        var midashiId = int.Parse(midashi.Id.Replace("midashi", ""));
                        if ((midashiId - previousMidashiId) == 100)
                        {
                            epubDocument.Chapters.Add(new Chapter() { Title = TextProcess(midashi) });
                            hasChapter = true;
                        }
                        else if ((midashiId - previousMidashiId) == 10)
                        {
                            epubDocument.EnsureChapter();
                            epubDocument.Chapters[^1].Sections.Add(new Section(TextProcess(midashi)));
                            hasSection = true;
                        }
                        contentsIds.Add(midashiId);
                        previousMidashiId = midashiId;
                    }
                }
            }
            else
            {
                epubDocument.Chapters.Add(new Chapter()
                {
                    Title = null,
                    Sections = [new Section(epubDocument.Title)]
                });
            }
            return (contentsIds, hasChapter, hasSection);
        }

        /// <summary>
        /// 新規状態のときに初期設定を行います
        /// </summary>
        private static (int focusChapterIdx, int focusSectionIdx) SetChapterAndSection(EpubDocument document, bool hasChapter, bool hasSection, int chapterNum, int sectionNum)
        {
            if (chapterNum == -1)
            {
                if (hasChapter)
                {
                    document.Chapters.Insert(0, new Chapter());
                }
                chapterNum++;
                sectionNum = -1;
            }
            if (sectionNum == -1)
            {
                if (hasSection)
                {
                    document.EnsureChapter();
                    document.Chapters[^1].Sections.Insert(0, new Section("___"));
                }
                sectionNum++;
            }
            return (chapterNum, sectionNum);
        }

        private static string GetCardUrl(string url)
        {
            return UrlBookToCard().Replace(url, "$1card$2$3");
        }

        /// <summary>
        /// class="main_text"なdiv要素の内容を<paramref name="document"/>に書き込む
        /// </summary>
        /// <param name="document">書き込むEpubDocument</param>
        /// <param name="mainText">class = "main_text" なdiv要素</param>
        internal void ProcessMainText(EpubDocument document, IHtmlDivElement mainText)
        {
            // 青空文庫の見出しのaタグのidの数値に対応
            int headingId = 0;
            SplittedLineBuilder paragraphLineBuilder = new();
            SplittedLineBuilder scriptLineLineBuilder = new();
            // 作品中で使われるCSSスタイルを実現するために必要なclassの情報を保持する。
            // 例:
            // 字下げに使われる class "jisage_1", "jisage_2", ..., "jisage_n"で、 n がいくつになるかは、その作品全体をチェックしないとわからないため、
            Dictionary<string, (int min, int max)> classes = new();

            //ProcessChildren(); する。
        }

        /// <summary>
        /// EpubDocumentに対してある要素に応じた処理を行う。
        /// </summary>
        /// <param name="document">処理対象のEpubDocument</param>
        /// <param name="element">処理を行う要素</param>
        /// <param name="appliedClasses">適用されるclassのリスト</param>
        /// <param name="scrapingInfo"></param>
        internal void ProcessChildren(EpubDocument document, IElement element, string appliedClasses, ref int headingId, SplittedLineBuilder paragraphLineBuilder, SplittedLineBuilder scriptLineLineBuilder, Dictionary<string, (int min, int max)> classes)
        {

        }

        /// <summary>
        /// <see cref="Classes"/>に基づき、EpubDocument内で使用するクラスを生成する。
        /// </summary>
        /// <param name="document"><see cref="CssClass"/>を変更するEpubDocument</param>
        void AddCssClasses(EpubDocument document, Dictionary<string, (int min, int max)> classes)
        {
            (int min, int max) value = (0, 0);
            if (classes.TryGetValue("jisage", out value))
            {
                for (int i = value.min; i <= value.max; i++)
                {
                    document.CssClasses.Add(new CssClass("jisage", $@"
                    .jisage_{i} {{
                        margin-left: {i}em;
                    }}
                    "));
                }
            }
            if (classes.TryGetValue("text_indent", out value))
            {
                for (int i = value.min; i <= value.max; i++)
                {
                    document.CssClasses.Add(new CssClass("text_indent", $@"
                    .text_indent_{i} {{
                    text-indent: {i}em;
                    }}
                    "));
                }
            }
            if (classes.TryGetValue("chitsuki", out value))
            {
                for (int i = value.min; i <= value.max; i++)
                {
                    document.CssClasses.Add(new CssClass("chitsuki", $@"
                    .chitsuki_{i} {{
                        text-align: right;
                        margin-right: {i}em;
                    }}
                    "));
                }
            }
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
