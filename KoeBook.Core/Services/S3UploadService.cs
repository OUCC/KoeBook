using Amazon.S3;
using Amazon.S3.Transfer;
using KoeBook.Core.Contracts.Services;

namespace KoeBook.Core.Services;

public class S3UploadService(IAmazonS3 s3Client) : IS3UploadService
{
    private readonly IAmazonS3 _s3Client = s3Client;

    public async ValueTask<string> UploadFileAsync(string filePath, string title, CancellationToken cancellationToken)
    {
        try
        {
            // 設定に移すのが面倒なので固定値
            const string S3BucketName = "koebook-gakusai-storage";
            var guid = Guid.NewGuid();
            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(filePath, S3BucketName, $"{guid}/{title}.epub", cancellationToken);

            return $"http://storage.koebook.oucc.org/{guid}/{Uri.EscapeDataString(title)}.epub";
        }
        catch (AmazonS3Exception e)
        {
            throw new EbookException(ExceptionType.S3UploadFailed, innerException: e);
        }
    }
}
