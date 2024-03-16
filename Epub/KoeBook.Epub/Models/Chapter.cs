namespace KoeBook.Epub.Models;

public class Chapter
{
    public List<Section> Sections { get; init; } = [];
    public string? Title { get; set; }
}
