using System.Text;

namespace KoeBook.Epub.Models;

public abstract class ScrapingBase
{
    protected List<StringBuilder> stringBuilders = new();

    internal void AddText(string text)
    {
        stringBuilders[^1].Append(text);
    }

    internal void AddText(List<string> texts)
    {
        stringBuilders[^1].Append(texts[0]);
        for (int i = 1; i < texts.Count; i++)
        {
            stringBuilders.Add(new StringBuilder(texts[i]));
        }
    }   

    internal List<string> GetText()
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
