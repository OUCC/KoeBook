using KoeBook.Core.Models;

namespace KoeBook.Epub.Models;

public sealed class Paragraph : Element, IAsyncDisposable
{
    private bool _disposed;
    public ScriptLine? ScriptLine { get; set; }
    public Audio? Audio => ScriptLine?.Audio;
    public string Text { get; set; } = "";

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (Audio != null)
        {
            await Audio.DisposeAsync();
        }
        _disposed = true;
    }
}
