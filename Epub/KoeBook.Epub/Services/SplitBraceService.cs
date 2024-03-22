using KoeBook.Epub.Contracts.Services;

namespace KoeBook.Epub.Services;

public class SplitBraceService : ISplitBraceService
{
    public IEnumerable<string> SplitBrace(string text)
    {
        // textが空白だった時 paragraph を挿入する処理をスキップ
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        if (text.Length == 1)
        {
            yield return text;
            yield break;
        }

        var bracket = 0;
        var brackets = new int[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '「' || c == '『') bracket++;
            else if (c == '」' || c == '』') bracket--;
            brackets[i] = bracket;
        }

        var mn = Math.Min(0, brackets.Min());
        var startIdx = 0;
        for (var i = 0; i < brackets.Length; i++)
        {
            brackets[i] -= mn;
            if ((text[i] == '「' || text[i] == '『') && brackets[i] == 1 && i != 0 && startIdx != i)
            {
                yield return text[startIdx..i];
                startIdx = i;
            }
            if ((text[i] == '」' || text[i] == '』') && brackets[i] == 0)
            {
                yield return text[startIdx..(i + 1)];
                startIdx = i + 1;
            }
        }
        if (startIdx != text.Length)
        {
            yield return text[startIdx..];
        }
    }

    public IEnumerable<string> SplitBrace(IEnumerable<string> texts)
    {
        foreach (var text in texts)
        {
            var results = SplitBrace(text);
            foreach (var result in results)
            {
                yield return result;
            }
        }
    }
}
