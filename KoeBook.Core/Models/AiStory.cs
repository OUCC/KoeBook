using System.Xml.Serialization;
using KoeBook.Core.Utilities;

namespace KoeBook.Models;

[XmlRoot("Book", IsNullable = false)]
public record AiStory(
    [XmlElement("Title", typeof(string), IsNullable = false)] string Title,
    [XmlArray("Content", IsNullable = false), XmlArrayItem("Section", IsNullable = false)] AiStory.Section[] Sections)
{
    public record Section(
        [XmlElement("Paragraph", IsNullable = false)] Paragraph[] Paragraphs);


    public record Paragraph(
        [XmlElement("Text", typeof(TextElement), IsNullable = false), XmlElement("Ruby", typeof(Ruby), IsNullable = false)] InlineElement[] Inlines)
    {
        public string GetText() => string.Concat(Inlines.Select(e => e.Text));

        public string GetScript() => string.Concat(Inlines.Select(e => e.Script));
    }

    public abstract record class InlineElement
    {
        public abstract string Text { get; }
        public abstract string Script { get; }
    }

    public record TextElement([XmlText] string InnerText) : InlineElement
    {
        public override string Text => InnerText;
        public override string Script => InnerText;
    }


    public record Ruby(
        [XmlElement("Rb", IsNullable = false)] string Rb,
        [XmlElement("Rt", IsNullable = false)] string Rt) : InlineElement
    {
        public override string Text => Rb;
        public override string Script => Rt;
    }

}
