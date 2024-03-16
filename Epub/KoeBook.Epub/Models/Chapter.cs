namespace KoeBook.Epub.Models;

public class Chapter
{
    public List<Section> Sections { get; } = [];
    public string? Title { get; set; }
}
