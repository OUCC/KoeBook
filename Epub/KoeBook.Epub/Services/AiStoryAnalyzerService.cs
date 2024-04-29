using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using KoeBook.Models;

namespace KoeBook.Epub.Services;

public partial class AiStoryAnalyzerService(ISplitBraceService splitBraceService)
{
    private readonly ISplitBraceService _splitBraceService = splitBraceService;


    public EpubDocument CreateEpubDocument(AiStory aiStory, Guid id)
    {
        int sectionNumber = 1;
        return new EpubDocument(aiStory.Title, "AI", "", id)
        {
            Chapters = [new Chapter()
            {
                Sections = aiStory.Sections.Select(s => new Section($"第{sectionNumber++}章")
                {
                    Elements = s.Paragraphs.SelectMany(p =>
                    _splitBraceService.SplitBrace(p.GetText())
                        .Zip(_splitBraceService.SplitBrace(p.GetScript()))
                        .Select(Element (p) => new Paragraph
                        {
                            Text = p.First,
                            ScriptLine = new(p.Second, "", "")
                        })
                    ).ToList(),
                }).ToList(),
            }]
        };
    }
}
