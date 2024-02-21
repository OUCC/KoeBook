﻿using System;
using NAudio.Wave;

namespace KoeBook.Epub;

public sealed class Audio
{
    public TimeSpan TotalTime { get; }
    private byte[] _mp3Data = [];

    public Audio(byte[] mp3Data)
    {
        _mp3Data = mp3Data;
        using var ms = new MemoryStream();
        ms.Write(_mp3Data.AsSpan());
        ms.Flush();
        ms.Position = 0;
        using var reader = new Mp3FileReader(ms);
        TotalTime = reader.TotalTime;
    }
}
