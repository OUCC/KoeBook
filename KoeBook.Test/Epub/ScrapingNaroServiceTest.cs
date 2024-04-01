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

    [Fact]
    public async Task ReadPageAsync()
    {
        Directory.CreateDirectory("./tmp/img");
        var config = Configuration.Default.WithDefaultLoader();
        using var context = BrowsingContext.New(config);
        using var doc = await context.OpenAsync(req => req.Content(File.ReadAllText("./TestData/Naro/n0000aa/1.html")));

        var (chapterTitle, section) = await _scrapingNaroService.ReadPageAsync(doc, true, "./tmp/img", default);

        Assert.Null(chapterTitle);
        Assert.Equal("タイトル1", section.Title);
        var elements = section.Elements;
        Assert.Equal(6, elements.Count);
        var text = Assert.IsType<Paragraph>(elements[0]);
        Assert.Equal("名前は【<ruby><rb>佐久平</rb><rp>《</rp><rt>さくだいら</rt><rp>》</rp></ruby>　<ruby><rb>啓介</rb><rp>《</rp><rt>けいすけ</rt><rp>》</rp></ruby>】。", text.Text);
        text = Assert.IsType<Paragraph>(elements[1]);
        Assert.Equal("　テストテストテストテストテスト", text.Text);
        text = Assert.IsType<Paragraph>(elements[2]);
        Assert.Equal("「セリフセリフセリフセリフ！」", text.Text);
        text = Assert.IsType<Paragraph>(elements[3]);
        Assert.Equal("テスト", text.Text);
        text = Assert.IsType<Paragraph>(elements[4]);
        Assert.Equal("「インラインテスト」", text.Text);
        text = Assert.IsType<Paragraph>(elements[5]);
        Assert.Equal("テスト。ほげほげほげほげほげ。", text.Text);
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
