using KoeBook.Core.Contracts.Services;

namespace KoeBook.Test;

internal partial class MockCreateCoverFileService : ICreateCoverFileService
{
    public void Create(string title, string author, string coverFilePath)
    {
        using var fs = File.Create(coverFilePath);
        fs.Write(CoverFile.ToArray());
        fs.Flush();
    }
}
