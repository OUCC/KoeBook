using System.Xml.Serialization;

namespace KoeBook.Models;

[XmlRoot("Book")]
public record AiStory(
    [property: XmlElement("Title", typeof(string), IsNullable = false)] string Title,
    [property: XmlArray("Content", IsNullable = false), XmlArrayItem("Section", IsNullable = false)] AiStory.Section[] Sections)
{
    private AiStory() : this("", []) { }

    public record Section(
        [property: XmlArrayItem("Paragraph", IsNullable = false)] Paragraph[] Paragraphs)
    {
        private Section() : this([]) { }
    }


    public record Paragraph(
        [property: XmlElement("Text", typeof(TextElement), IsNullable = false), XmlElement("Ruby", typeof(Ruby), IsNullable = false)] InlineElement[] Inlines)
    {
        private Paragraph() : this([]) { }

        public string GetText() => string.Concat(Inlines.Select(e => e.Text));

        public string GetScript() => string.Concat(Inlines.Select(e => e.Script));
    }

    public abstract record class InlineElement
    {
        public abstract string Text { get; }
        public abstract string Script { get; }
    }

    public record TextElement([property: XmlText] string InnerText) : InlineElement
    {
        private TextElement() : this("") { }

        public override string Text => InnerText;
        public override string Script => InnerText;
    }

    public record Ruby(
        [property: XmlElement("Rb", IsNullable = false)] string Rb,
        [property: XmlElement("Rt", IsNullable = false)] string Rt) : InlineElement
    {
        private Ruby() : this("", "") { }

        public override string Text => Rb;
        public override string Script => Rt;
    }
}
