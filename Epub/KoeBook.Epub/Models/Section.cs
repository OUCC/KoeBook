using System.Text;

namespace KoeBook.Epub.Models;

public sealed class Section(string title)
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = title;
    public List<Element> Elements { get; set; } = [];

    public TimeSpan GetTotalTime()
    {
        var time = TimeSpan.Zero;
        foreach (var element in Elements)
        {
            if (element is Paragraph para && para.Audio != null)
            {
                time += para.Audio.TotalTime;
            }
        }
        return time;
    }
}
