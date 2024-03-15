namespace KoeBook.Epub.Contracts.Services;

public interface IScrapingHelperService
{
    List<string> SplitBrace(string text);
    List<string> SplitBrace(List<string> texts);
    void AddText(string text);
    void AddText(List<string> texts);
    List<string> GetText();
}
