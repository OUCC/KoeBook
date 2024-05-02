namespace KoeBook.Core.Contracts.Services;

public interface IS3UploadService
{
    ValueTask<string> UploadFileAsync(string filePath, CancellationToken cancellationToken);
}
