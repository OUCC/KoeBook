using KoeBook.Models;

namespace KoeBook.Core.Models;

/// <summary>
/// 読み上げる本の情報
/// </summary>
public class BookProperties
{
    public BookProperties(Guid id, string source, SourceType sourceType)
    {
        if (sourceType != SourceType.FilePath && sourceType != SourceType.Url)
            throw new ArgumentException($"{nameof(sourceType)}は{nameof(SourceType.FilePath)}か{nameof(SourceType.Url)}である必要があります。");
        Id = id;
        Source = source;
        SourceType = sourceType;
    }

    public BookProperties(Guid id, AiStory aiStory)
    {
        Id = id;
        Source = aiStory;
        SourceType = SourceType.AiStory;
    }

    public Guid Id { get; }

    /// <summary>
    /// UriまたはAiStory
    /// </summary>
    public object Source { get; }

    public SourceType SourceType { get; }
}
