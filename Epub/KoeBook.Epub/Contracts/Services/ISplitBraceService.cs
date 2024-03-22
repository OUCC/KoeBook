namespace KoeBook.Epub.Contracts.Services;

public interface ISplitBraceService
{
    IEnumerable<string> SplitBrace(string text);
    IEnumerable<string> SplitBrace(IEnumerable<string> texts);
}
