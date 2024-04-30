using System.Collections.Immutable;

namespace KoeBook.Core.Models;

/// <summary>
/// 本の読み上げ情報
/// </summary>
public class BookScripts(BookProperties bookProperties, BookOptions options)
{
    public BookProperties BookProperties { get; } = bookProperties;

    /// <summary>
    /// 本の読み上げ設定
    /// </summary>
    public BookOptions Options { get; } = options;

    /// <summary>
    /// 読み上げテキストの配列
    /// </summary>
    public required ImmutableArray<ScriptLine> ScriptLines { get; set; }
}
