using System.Text;

namespace KoeBook.Core.Utilities;

public class SplittedLineBuilder
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly List<string> _texts = [];

    public void Append(string text)
    {
        _stringBuilder.Append(text);
    }

    /// <summary>
    /// すでに分割済みの行を追加します
    /// </summary>
    /// <param name="lines"></param>
    public void Append(IEnumerable<string> lines)
    {
        foreach (var (value, isFirst, isLast) in lines.WithPosition())
        {
            if (isLast)
            {
                _stringBuilder.Append(value);

            }
            else if (isFirst)
            {
                _stringBuilder.Append(value);
                _texts.Add(_stringBuilder.ToString());
                _stringBuilder.Clear();
            }
            else
            {
                _texts.Add(value);
            }
        }
    }

    public string[] ToLinesAndClear()
    {
        var result = _stringBuilder.Length == 0 ? _texts.ToArray() : [.. _texts, _stringBuilder.ToString()];

        _stringBuilder.Clear();
        _texts.Clear();
        return result;
    }
}
