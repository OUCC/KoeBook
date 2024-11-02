namespace KoeBook.Core.Contracts.Services;

public interface ITranscodingService
{
    public ValueTask<(Stream, TimeSpan)> TranscodeAsync(IAsyncEnumerable<byte[]> source, CancellationToken cancellationToken);
}
