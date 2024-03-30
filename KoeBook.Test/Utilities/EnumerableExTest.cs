using KoeBook.Core.Utilities;

namespace KoeBook.Test.Utilities;

public class EnumerableExTest
{
    [Theory]
    [InlineData(new[] { 0 })]
    [InlineData(new[] { 0, 1 })]
    [InlineData(new[] { 0, 1, 2 })]
    [InlineData(new[] { 0, 1, 2, 3 })]
    public void WithPosition(int[] input)
    {
        var result = input.WithPosition();

        Assert.Equal(input, result.Select(v => v.value));
        Assert.True(result.First().isFirst);
        Assert.All(result.Skip(1), (v) => Assert.False(v.isFirst));
        Assert.True(result.Last().isLast);
        Assert.All(result.SkipLast(1), (v) => Assert.False(v.isLast));
    }

    [Fact]
    public void WithPosition_Empty()
    {
        var result = Array.Empty<int>().WithPosition();

        Assert.Empty(result);
    }
}
