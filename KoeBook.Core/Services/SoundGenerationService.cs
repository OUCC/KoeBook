using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using NAudio.Wave;

namespace KoeBook.Core.Services;

public class SoundGenerationService(
    IStyleBertVitsClientService styleBertVitsClientService,
    ISoundGenerationSelectorService soundGenerationSelectorService) : ISoundGenerationService
{
    private readonly IStyleBertVitsClientService _styleBertVitsClientService = styleBertVitsClientService;
    private readonly ISoundGenerationSelectorService _soundGenerationSelectorService = soundGenerationSelectorService;

    public async ValueTask<byte[]> GenerateLineSoundAsync(ScriptLine scriptLine, BookOptions bookOptions, CancellationToken cancellationToken)
    {
        var model = bookOptions.CharacterMapping[scriptLine.Character];
        var soundModel = _soundGenerationSelectorService.Models.FirstOrDefault(m => m.Name == model)
            ?? throw new EbookException(ExceptionType.SoundGenerationFailed);
        var style = soundModel.Styles.Contains(scriptLine.Style) ? scriptLine.Style : soundModel.Styles[0];
        using var msWriter = new MemoryStream();
        WaveFileWriter? writer = null;
        byte[] dataBuffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            await foreach (var voice in GenerateSoundAsync(scriptLine.Text, style, soundModel.Id, cancellationToken))
            {
                if (voice.Length > dataBuffer.Length)
                {
                    ArrayPool<byte>.Shared.Return(dataBuffer);
                    dataBuffer = ArrayPool<byte>.Shared.Rent(voice.Length);
                }
                using var msReader = new MemoryStream();
                await msReader.WriteAsync(voice, cancellationToken);
                msReader.Position = 0;
                using var reader = new WaveFileReader(msReader);
                var read = await reader.ReadAsync(dataBuffer, cancellationToken);
                if (writer is null)
                {
                    writer = new WaveFileWriter(msWriter, reader.WaveFormat);
                }
                await writer.WriteAsync(dataBuffer.AsMemory()[..read], cancellationToken);
            }
            if (writer is null)
            {
                throw new EbookException(ExceptionType.SoundGenerationFailed);
            }
            await writer.FlushAsync(cancellationToken);
            return msWriter.ToArray();
        }
        catch { throw; }
        finally
        {
            ArrayPool<byte>.Shared?.Return(dataBuffer);
            writer?.Dispose();
        }
    }

    private async IAsyncEnumerable<byte[]> GenerateSoundAsync(string text, string style, string modelId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var l in SplitPeriod(text, 300))
        {
            var queryCollection = HttpUtility.ParseQueryString(string.Empty);
            queryCollection.Add("text", l);
            queryCollection.Add("model_id", modelId);
            queryCollection.Add("style", style);
            yield return await _styleBertVitsClientService
                .GetAsByteArrayAsync($"/voice?{queryCollection}", ExceptionType.SoundGenerationFailed, cancellationToken).ConfigureAwait(false);
        }
    }

    private IEnumerable<string> SplitPeriod(string text, int limit)
    {
        if (text.Length < limit)
        {
            yield return text;
        }
        else
        {
            List<int> periodList = [0];
            var textSpan = text.AsSpan();
            var chunk = textSpan[..limit];
            while (true)
            {
                var periodIndex = periodList[^1] + chunk.LastIndexOf('。') + 1;
                periodList.Add(periodIndex);
                var nextEnd = periodIndex + limit;
                if (nextEnd < textSpan.Length)
                {
                    chunk = textSpan[periodIndex..nextEnd];
                }
                else
                {
                    periodList.Add(textSpan.Length);
                    break;
                }
            }
            for (var i = 1; i < periodList.Count; i++)
            {
                yield return text[periodList[i - 1]..periodList[i]];
            }
        }
    }
}
