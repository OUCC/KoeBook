namespace KoeBook.Core.Utilities;

public static class EnumerableEx
{
    public static IEnumerable<(TSource value, bool isFirst, bool isLast)> WithPosition<TSource>(this IEnumerable<TSource> source)
    {
        using var enumerator = source.GetEnumerator();

        var hasNext = enumerator.MoveNext();
        if (!hasNext)
            yield break;
        var current = enumerator.Current;
        hasNext = enumerator.MoveNext();
        yield return (current, true, !hasNext);

        while (hasNext)
        {
            current = enumerator.Current;
            hasNext = enumerator.MoveNext();
            yield return (current, false, !hasNext);
        }
    }
}
