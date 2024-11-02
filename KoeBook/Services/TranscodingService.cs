using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage.Streams;

namespace KoeBook.Services;

public class TranscodingService : ITranscodingService
{
    public async ValueTask<(Stream, TimeSpan)> TranscodeAsync(IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        var mediaSource = await CreateAudioSourceAsync(source, cancellationToken);

        var transcoder = new MediaTranscoder();
        var randomAccessStream = new InMemoryRandomAccessStream();
        await randomAccessStream.FlushAsync();
        var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto);
        var preOpt = await transcoder.PrepareMediaStreamSourceTranscodeAsync(mediaSource, randomAccessStream, profile).AsTask(cancellationToken);

        if (!preOpt.CanTranscode)
            throw new EbookException(ExceptionType.SoundGenerationFailed);

        var transcodeOpt = preOpt.TranscodeAsync();

        await transcodeOpt.AsTask();

        return (randomAccessStream.AsStreamForRead(), GetAudioDuration(randomAccessStream));
    }

    private async ValueTask<IMediaSource> CreateAudioSourceAsync(IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken)
    {
        MediaStreamSource? mediaStream = null;
        await foreach (var audio in source.ConfigureAwait(false).WithCancellation(cancellationToken))
        {
            var media = AudioEncodingProperties.CreatePcm(44100, 1, 16);
            media.SetFormatUserData(audio);
            if (mediaStream is null)
                mediaStream = new MediaStreamSource(new AudioStreamDescriptor(media));
            else
                mediaStream.AddStreamDescriptor(new AudioStreamDescriptor(media));
        }
        return mediaStream ??
              new MediaStreamSource(new AudioStreamDescriptor(AudioEncodingProperties.CreatePcm(44100, 1, 16)));
    }

    private TimeSpan GetAudioDuration(IRandomAccessStream mediaStream)
    {
        var mediaSource = MediaSource.CreateFromStream(mediaStream, "audio/mpeg");
        return mediaSource.Duration!.Value;
    }
}
