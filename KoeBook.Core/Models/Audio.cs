using System.IO;
using NAudio.Wave;

namespace KoeBook.Epub.Models;

public sealed class Audio(TimeSpan totalTIme, string tempFilePath)
{
    public TimeSpan TotalTime { get; } = totalTIme;
    public string TempFilePath { get; } = tempFilePath;

    public FileStream GetStream()
    {
        return new FileStream(TempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
    }
}
