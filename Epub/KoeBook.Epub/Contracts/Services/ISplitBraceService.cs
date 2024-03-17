namespace KoeBook.Epub.Contracts.Services;

public interface ISplitBraceService
{
    List<string> SplitBrace(string text);
    List<string> SplitBrace(List<string> texts);
}
