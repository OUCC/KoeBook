using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace KoeBook.Epub.Models;

public class EpubDocument(string title, string author, Guid id, string coverFilePath = "")
{
    public string Title { get; set; } = title;
    public string Author { get; set; } = author;

    public string CoverFilePath { get; set; } = coverFilePath;

    public Guid Id { get; } = id;

    public List<CssClass> CssClasses { get; set; } = [
        new CssClass("-epub-media-overlay-active", """
            .-epub-media-overlay-active, .-epub-media-overlay-active * {
            	background-color: yellow;
            	color: black !important;
            }
            """),
        new CssClass("-epub-media-overlay-unactive", """
            html.-epub-media-overlay-unactive * {
                color: gray;
            }
            """),
    ];
    public List<Chapter> Chapters { get; set; } = [];

    internal void EnsureChapter()
    {
        if (Chapters.Count == 0)
            Chapters.Add(new Chapter() { Title = null });
    }

    internal void EnsureSection(int chapterIndex)
    {
        EnsureChapter();

        if (Chapters[chapterIndex].Sections.Count == 0)
        {
            if (Chapters[chapterIndex].Title != null)
                Chapters[chapterIndex].Sections.Add(new Section(Chapters[chapterIndex].Title!));
            else
                Chapters[chapterIndex].Sections.Add(new Section(Title));
        }
    }

    internal void EnsureParagraph(int chapterIndex, int sectionIndex)
    {
        EnsureSection(chapterIndex);

        if (Chapters[chapterIndex].Sections[sectionIndex].Elements.Count == 0)
            Chapters[chapterIndex].Sections[sectionIndex].Elements.Add(new Paragraph());
    }
}
