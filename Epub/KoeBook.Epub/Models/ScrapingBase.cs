using System.Text;

namespace KoeBook.Epub.Models;

public abstract class ScrapingBase
{
    protected StringBuilder stringBuilder = new();

    internal void AddText(string text)
    {
        stringBuilder.Append(text);
    }

    internal string GetText()
    {
        var result = stringBuilder.ToString();
        stringBuilder.Clear();
        return result;
    }
}
