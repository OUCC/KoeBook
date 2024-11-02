using System.IO;
using NAudio.Wave;

namespace KoeBook.Epub.Models;

public sealed class Audio(TimeSpan totalTIme, Stream stream) : IAsyncDisposable
{
    private bool _disposed;
    public TimeSpan TotalTime { get; } = totalTIme;
    public Stream? AudioStream { get; private set; } = stream;

    public async ValueTask DisposeAsync()
    {
        if (_disposed) { return; }
        if (AudioStream != null)
        {
            await AudioStream.DisposeAsync().ConfigureAwait(false);
            AudioStream = null;
        }
        _disposed = true;
    }
}
