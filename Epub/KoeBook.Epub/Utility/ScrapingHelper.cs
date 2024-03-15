namespace KoeBook.Epub.Utility;

public static class ScrapingHelper
{
    public static List<string> SplitBrace(string text)
    {
        // textが空白だった時paragraphを挿入する処理をスキップ
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

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

    public static List<string> SplitBrace(List<string> texts)
    {
        var result = new List<string>();
        foreach (var text in texts)
        {
            result.AddRange(SplitBrace(text));
        }
        return result;
    }
}
