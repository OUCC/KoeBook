
namespace KoeBook.Epub.Models;

public sealed class Chapter : IAsyncDisposable
{
    private bool _disposed;
    public List<Section> Sections { get; init; } = [];
    public string? Title { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        var tasks = Sections.Select(async s => await s.DisposeAsync());
        await Task.WhenAll(tasks);
        _disposed = true;
    }
}
