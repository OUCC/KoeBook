using System.Text;
using KoeBook.Epub.Contracts.Services;

namespace KoeBook.Epub.Services;

public class SplitBraceService : ISplitBraceService
{
    private List<StringBuilder> stringBuilders = new();

    public List<string> SplitBrace(string text)
    {
        // textが空白だった時 paragraph を挿入する処理をスキップ
        if (string.IsNullOrWhiteSpace(text))
            return [];

        if (text.Length == 1 && (text == "「" || text == "『" || text == "」" || text == "』"))
            return [text];

        var bracket = 0;
        var brackets = new int[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '「' || c == '『') bracket++;
            else if (c == '」' || c == '』') bracket--;
            brackets[i] = bracket;
        }

        var result = new List<string>();
        var mn = Math.Min(0, brackets.Min());
        var startIdx = 0;
        for (var i = 0; i < brackets.Length; i++)
        {
            brackets[i] -= mn;
            if ((text[i] == '「' || text[i] == '『') && brackets[i] == 1 && i != 0 && startIdx != i)
            {
                result.Add(text[startIdx..i]);
                startIdx = i;
            }
            if ((text[i] == '」' || text[i] == '』') && brackets[i] == 0)
            {
                result.Add(text[startIdx..(i + 1)]);
                startIdx = i + 1;
            }
        }
        if (startIdx != text.Length)
        {
            result.Add(text[startIdx..]);
        }
        return result;
    }

    public List<string> SplitBrace(List<string> texts)
    {
        var result = new List<string>();
        foreach (var text in texts)
        {
            result.AddRange(SplitBrace(text));
        }
        return result;
    }

    public void AddText(string text)
    {
        stringBuilders[^1].Append(text);
    }

    public void AddText(List<string> texts)
    {
        stringBuilders[^1].Append(texts[0]);
        for (int i = 1; i < texts.Count; i++)
        {
            stringBuilders.Add(new StringBuilder(texts[i]));
        }
    }

    public List<string> GetText()
    {
        List<string> result = new List<string>();
        foreach (StringBuilder stringBuilder in stringBuilders)
        {
            result.Add(stringBuilder.ToString());
        }
        stringBuilders.Clear();
        return result;
    }
}
