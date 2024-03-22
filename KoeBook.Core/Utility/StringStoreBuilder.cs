using System.Text;
using KoeBook.Core.Utility;

namespace KoeBook.Core.Utility
{
    public class StringStoreBuilder
    {
        private StringBuilder _stringBuilder = new();
        private List<string> _texts = new List<string>();

        public void Store(string text)
        {
            _stringBuilder.Append(text);
        }

        public void Store(IEnumerable<string> texts)
        {
            var (first, rest) = texts.FirstWithRest();
            _stringBuilder.Append(first);
            _texts.Add(_stringBuilder.ToString());
            foreach (var (value, isLast) in rest.WithLastFlag())
            {
                if (isLast)
                {
                    _stringBuilder.Length = 0;
                    _stringBuilder.Append(value);
                }
                else
                {
                    _texts.Add(value);
                }
            }
        }

        public IEnumerable<string> Release()
        {
            foreach (var text in _texts)
            {
                yield return text;
            }
            yield return _stringBuilder.ToString();
            _stringBuilder.Clear();
            _texts.Clear();
        }
    }
}
