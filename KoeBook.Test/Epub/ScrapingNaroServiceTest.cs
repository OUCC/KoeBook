using KoeBook.Core;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
