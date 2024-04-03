using System.Runtime.CompilerServices;
using AngleSharp;
using AngleSharp.Dom;
using KoeBook.Epub.Services;

namespace KoeBook.Test.Epub;

public class ScrapingAozoraServiceTest
{
    [Theory]
    [InlineData("", "")]
    public async Task TextProcess(string input, string expected)
    {
        using var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        using var doc = await context.OpenAsync(req => req.Content(input));

        Assert.NotNull(doc.ParentElement);
        var result = ScrapingAozora.TextProcess(doc.ParentElement!);

        Assert.Equal(expected, result);
    }
}

file static class ScrapingAozora
{
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    private static extern string TextProcess(ScrapingAozoraService? _, IElement element);

    public static string TextProcess(IElement element) => TextProcess(null, element);
}
