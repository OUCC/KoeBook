using System.Text.Json;
using AngleSharp;
using KoeBook.Core;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using KoeBook.Epub.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KoeBook.Test.Epub;

public class ScrapingNaroServiceTest : DiTestBase
{
    private readonly ScrapingNaroService _scrapingNaroService;

    public ScrapingNaroServiceTest()
    {
        _scrapingNaroService = Host.Services
            .GetServices<IScrapingService>()
            .OfType<ScrapingNaroService>()
            .Single();
    }

    [Theory]
    [InlineData("n0000aa", null, "タイトル1", new[] {
            "名前は【<ruby><rb>佐久平</rb><rp>《</rp><rt>さくだいら</rt><rp>》</rp></ruby>　<ruby><rb>啓介</rb><rp>《</rp><rt>けいすけ</rt><rp>》</rp></ruby>】。",
            "　テストテストテストテストテスト",
            "「セリフセリフセリフセリフ！」",
            "テスト",
            "「インラインテスト」",
            "テスト。ほげほげほげほげほげ。",
        })]
    [InlineData("n0000ab", "プロローグ", "タイトル1", new[] {
            "名前は【<ruby><rb>佐久平</rb><rp>《</rp><rt>さくだいら</rt><rp>》</rp></ruby>　<ruby><rb>啓介</rb><rp>《</rp><rt>けいすけ</rt><rp>》</rp></ruby>】。",
            "　テストテストテストテストテスト",
            "「セリフセリフセリフセリフ！」",
            "テスト",
            "「インラインテスト」",
            "テスト。ほげほげほげほげほげ。",
        })]
    public async Task ReadPageAsync(string ncode, string? expectedChapterTitle, string expectedSectionTitle, string[] expectedContents)
    {
        Directory.CreateDirectory($"./tmp/img/{ncode}");
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        using var doc = await context.OpenAsync(req => req.Content(File.ReadAllText($"./TestData/Naro/{ncode}/1.html")));

        var (chapterTitle, section) = await _scrapingNaroService.ReadPageAsync(doc, true, "./tmp/img", default);

        Assert.Equal(expectedChapterTitle, chapterTitle);
        Assert.Equal(expectedSectionTitle, section.Title);
        Assert.Equal(expectedContents.Length, section.Elements.Count);
        Assert.All(section.Elements.Zip(expectedContents), v =>
        {
            var text = Assert.IsType<Paragraph>(v.First);
            Assert.Equal(v.Second, text.Text);
        });
    }

    [Theory]
    [InlineData("https://ncode.syosetu.com/n0000a", "n0000a")]
    [InlineData("https://ncode.syosetu.com/n0000a/", "n0000a")]
    [InlineData("https://ncode.syosetu.com/n0000a/123", "n0000a")]
    [InlineData("https://ncode.syosetu.com/n0000a/123/", "n0000a")]
    [InlineData("https://ncode.syosetu.com/novelview/infotop/ncode/n0000a", "n0000a")]
    [InlineData("https://ncode.syosetu.com/novelview/infotop/ncode/n0000a/", "n0000a")]
    public void GetNcode_Success(string url, string? expected)
    {
        var result = ScrapingNaroService.GetNcode(url);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://ncode.syosetu.com/n0000あ")]
    [InlineData("https://ncode.syosetu.com/n0000あ/")]
    [InlineData("https://ncode.syosetu.com/n0000aあ/123")]
    [InlineData("https://ncode.syosetu.com/n0000aあ/123/")]
    public void GetNcode_Error(string url)
    {
        var exception = Record.Exception(() => ScrapingNaroService.GetNcode(url));

        var ebookException = Assert.IsType<EbookException>(exception);
        Assert.Equal(ExceptionType.InvalidUrl, ebookException.ExceptionType);
    }
}
