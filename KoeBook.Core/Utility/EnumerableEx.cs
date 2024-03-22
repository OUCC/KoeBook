namespace KoeBook.Core.Utility
{
    public static class EnumerableEx
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"><paramref name="source"/>の要素の型</typeparam>
        /// <returns>first: 指定されたシーケンスの最初の要素, rest: 残りの要素, 複数回のイテレートは保証しません</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>がnullです</exception>
        /// <exception cref="InvalidOperationException">ソース シーケンスが空です</exception>
        public static (TSource first, IEnumerable<TSource> rest) FirstWithRest<TSource>(this IEnumerable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
                throw new InvalidOperationException("ソース シーケンスが空です");

            return (enumerator.Current, GetRest(enumerator));

            static IEnumerable<T> GetRest<T>(IEnumerator<T> enumerator)
            {
                using (enumerator)
                {
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>がnullです</exception>
        /// <exception cref="InvalidOperationException">ソース シーケンスが空です</exception>
        public static IEnumerable<(TSource value, bool isLast)> WithLastFlag<TSource>(this IEnumerable<TSource> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            using var enumerator = source.GetEnumerator();

            var hasNext = enumerator.MoveNext();
            if (!hasNext)
                throw new InvalidOperationException("ソース シーケンスが空です");

            while (hasNext)
            {
                hasNext = enumerator.MoveNext();
                yield return (enumerator.Current, !hasNext);
            }
        }
    }
}
