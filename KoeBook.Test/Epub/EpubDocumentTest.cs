using KoeBook.Epub.Models;

namespace KoeBook.Test.Epub;

public class EpubDocumentTest
{
    #region Ensure List
    [Fact]
    public void EnsureChapter()
    {
        var document = new EpubDocument("title", "author", "cover", default);

        Assert.Empty(document.Chapters);

        // 空のときは追加
        document.EnsureChapter();

        var chapter = Assert.Single(document.Chapters);
        Assert.Null(chapter.Title);
        Assert.Empty(chapter.Sections);

        // 空でないときは無視
        document.EnsureChapter();

        var chapter2 = Assert.Single(document.Chapters);
        Assert.Same(chapter, chapter2);
    }

    [Fact]
    public void EnsureSection()
    {
        var document = new EpubDocument("title", "author", "cover", default);

        Assert.Empty(document.Chapters);

        // 空のときは追加される
        document.EnsureSection(0);

        var chapter = Assert.Single(document.Chapters);
        Assert.Null(chapter.Title);
        var section = Assert.Single(chapter.Sections);
        Assert.Equal("title", section.Title);
        Assert.Empty(section.Elements);

        // 空でないときは無視
        document.Chapters = [
            new()
            {
                Title = "chapter1",
                Sections = [
                    new("section1"),
                    new("section2"),
                    new("section3"),
                ],
            },
            new()
            {
                Title = "chapter2",
                Sections = [],
            },
        ];

        document.EnsureSection(0);

        Assert.Equal(3, document.Chapters[0].Sections.Count);

        document.EnsureSection(1);

        Assert.Equal("chapter2", document.Chapters[1].Sections[0].Title);

        // インデックスは正しく指定する必要がある
        var exception = Record.Exception(() => document.EnsureSection(5));

        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void EnsureParagraph()
    {
        var document = new EpubDocument("title", "author", "cover", default);

        Assert.Empty(document.Chapters);

        // 空のときは追加される
        document.EnsureParagraph(0, 0);

        var chapter = Assert.Single(document.Chapters);
        var section = Assert.Single(chapter.Sections);
        var element = Assert.Single(section.Elements);
        var paragraph = Assert.IsType<Paragraph>(element);
        Assert.Null(paragraph.Audio);
        Assert.Empty(paragraph.Text);
        Assert.Null(paragraph.ClassName);

        // 空でないときは無視
        document.Chapters = [
            new()
            {
                Title = "chapter1",
                Sections = [
                    new("section1")
                    {
                        Elements = [
                            new Paragraph()
                            {
                                Text = "paragraph1",
                            },
                        ]
                    },
                    new("section1")
                    {
                        Elements = []
                    },
                ],
            },
        ];

        document.EnsureParagraph(0, 0);

        chapter = Assert.Single(document.Chapters);
        Assert.Equal(2, chapter.Sections.Count);
        section = chapter.Sections[0];
        element = Assert.Single(section.Elements);
        paragraph = Assert.IsType<Paragraph>(element);
        Assert.Equal("paragraph1", paragraph.Text);

        document.EnsureParagraph(0, 1);

        element = Assert.Single(document.Chapters[0].Sections[1].Elements);
        paragraph = Assert.IsType<Paragraph>(element);
        Assert.Null(paragraph.Audio);
        Assert.Empty(paragraph.Text);
        Assert.Null(paragraph.ClassName);

        // インデックスは正しく指定する必要がある
        var exception = Record.Exception(() => document.EnsureParagraph(0, 5));

        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }
    #endregion
}
