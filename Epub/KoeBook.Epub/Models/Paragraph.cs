using KoeBook.Core.Models;

namespace KoeBook.Epub.Models;

public sealed class Paragraph : Element
{
    public ScriptLine? ScriptLine { get; set; }
    public Audio? Audio => ScriptLine?.Audio;
    public string? Text { get; set; }
}
