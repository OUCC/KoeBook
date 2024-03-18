namespace KoeBook.Epub.Contracts.Services;

public interface ISplitBraceService
{
    IEnumerable<string> SplitBrace(string text);
}
