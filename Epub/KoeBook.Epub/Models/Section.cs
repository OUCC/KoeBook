using System.Text;

namespace KoeBook.Epub.Models;

public sealed class Section(string title) : IAsyncDisposable
{
    private bool _disposed;
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = title;
    public List<Element> Elements { get; set; } = [];

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        var tasks = Elements.Where(e => e is Paragraph).Select(async p => await (p as Paragraph)!.DisposeAsync());
        await Task.WhenAll(tasks);
        _disposed = true;
    }

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
