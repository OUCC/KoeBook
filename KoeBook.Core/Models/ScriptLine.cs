using KoeBook.Epub.Models;

namespace KoeBook.Core.Models;

/// <summary>
/// 読み上げ1行分
/// </summary>
//public class ScriptLine(Paragraph paragraph, string text, string character, string style)
//{
//    /// <summary>
//    /// 読み上げ位置との関連付け
//    /// </summary>
//    public Paragraph Paragraph { get; } = paragraph;
public class ScriptLine(string text, string character, string style)
{
    public Audio? Audio { get; set; }

    /// <summary>
    /// 読み上げテキスト
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// 話者
    /// </summary>
    public string Character { get; set; } = character;

    /// <summary>
    /// 話者のスタイル
    /// </summary>
    public string Style { get; set; } = style;
}
