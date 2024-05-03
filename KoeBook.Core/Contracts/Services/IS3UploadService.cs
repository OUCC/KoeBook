namespace KoeBook.Core.Contracts.Services;

public interface IS3UploadService
{
    ValueTask<string> UploadFileAsync(string filePath, string title, CancellationToken cancellationToken);
}
