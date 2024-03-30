using System.Runtime.CompilerServices;
using System.Text;
using KoeBook.Core.Utilities;

namespace KoeBook.Test.Utilities
{
    public class SplittedLineBuilderTest
    {
        [Fact]
        public void Append_Single()
        {
            var builder = new SplittedLineBuilder();

            builder.Append("test");

            Assert.Equal("test", builder._stringBuilder().ToString());
            Assert.Empty(builder._texts());

            builder._texts().Add("test1");

            builder.Append("test2");

            Assert.Equal("testtest2", builder._stringBuilder().ToString());
            Assert.Equal(["test1"], builder._texts());
        }

        [Theory]
        [InlineData(new string[] { }, "builder", new string[] { })]
        [InlineData(new[] { "1" }, "builder1", new string[] { })]
        [InlineData(new[] { "1", "2" }, "2", new[] { "builder1" })]
        [InlineData(new[] { "1", "2", "3" }, "3", new[] { "builder1", "2" })]
        public void Append_Multiple(string[] input, string expectedBuilder, string[] expectedTexts)
        {
            var builder = new SplittedLineBuilder();
            builder._stringBuilder().Append("builder");

            builder.Append(input);

            Assert.Equal(expectedBuilder, builder._stringBuilder().ToString());
            Assert.Equal(expectedTexts, builder._texts());
        }

        [Theory]
        [InlineData("", new string[] { }, new string[] { })]
        [InlineData("", new[] { "test1" }, new[] { "test1" })]
        [InlineData("", new[] { "test1", "test2" }, new[] { "test1", "test2" })]
        [InlineData("", new[] { "test1", "test2", "test3" }, new[] { "test1", "test2", "test3" })]
        [InlineData("test0", new string[] { }, new[] { "test0" })]
        [InlineData("test0", new[] { "test1" }, new[] { "test1", "test0" })]
        [InlineData("test0", new[] { "test1", "test2" }, new[] { "test1", "test2", "test0" })]
        [InlineData("test0", new[] { "test1", "test2", "test3" }, new[] { "test1", "test2", "test3", "test0" })]
        public void ToLinesAndClear(string builderStr, string[] texts, string[] expected)
        {
            var builder = new SplittedLineBuilder();
            builder._stringBuilder().Append(builderStr);
            builder._texts().AddRange(texts);

            var result = builder.ToLinesAndClear();

            // 空になったかの判定はenumerateする前に行う
            Assert.Empty(builder._texts());
            Assert.Equal(0, builder._stringBuilder().Length);
            Assert.Equal(expected, result);
        }
    }
}


file static class Proxy
{
    [UnsafeAccessor(UnsafeAccessorKind.Field)]
    public static extern ref StringBuilder _stringBuilder(this SplittedLineBuilder builder);

    [UnsafeAccessor(UnsafeAccessorKind.Field)]
    public static extern ref List<string> _texts(this SplittedLineBuilder builder);
}
