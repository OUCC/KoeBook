using System.Runtime.CompilerServices;
using KoeBook.Epub.Services;

namespace KoeBook.Test.Epub;

public class AnalyzerServiceTest
{
    [Theory]
    [InlineData("aa", "aa")]
    [InlineData("<ruby><rb>漢字</rb><rp>（</rp><rt>かんじ</rt><rp>）</rp></ruby>", "かんじ")]
    [InlineData("ああ<ruby><rb>漢字</rb><rp>（</rp><rt>かんじ</rt><rp>）</rp></ruby>あああ", "ああかんじあああ")]
    [InlineData("""
        ああ<ruby><rb>漢字</rb><rp>（</rp><rt>かんじ</rt><rp>）</rp></ruby>あああ
        ああ<ruby><rb>漢字</rb><rp>（</rp><rt>かんじ</rt><rp>）</rp></ruby>あああ
        ああ<ruby><rb>漢字1</rb><rp>（</rp><rt>かんじ1</rt><rp>）</rp></ruby>あああ
        """, "ああかんじあああ\nああかんじあああ\nああかんじ1あああ")]
    [InlineData("<ruby> <rb>佐久平</rb> <rp>\n《 </rp> <rt>さくだいら</rt> <rp>》</rp>  </ruby>　<ruby><rb>啓介</rb><rp>《</rp><rt>けいすけ</rt><rp>》</rp></ruby>",
        "さくだいら　けいすけ")]
    [InlineData("<ruby><rb>漢字</rb>\n<rp>（</rp><rt>かんじ</rt><rp>）</rp></ruby>", "かんじ")]
    [InlineData("ああ<ruby><rb>漢字</rb><rp>（</rp><rt>かんじ</rt><rp>）</rp></ruby>あああ<ruby><rb>漢字</rb><rp>（</rp><rt>カンジ</rt><rp>）</rp></ruby>", "ああかんじあああカンジ")]
    public void ReplaceBaseTextWithRuby(string input, string expected)
    {
        var result = AnalyzerServiceProxy.ReplaceBaseTextWithRuby(null, input);

        Assert.Equal(expected, result);
    }
}

file static class AnalyzerServiceProxy
{
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern string ReplaceBaseTextWithRuby(AnalyzerService? _, string text);
}
