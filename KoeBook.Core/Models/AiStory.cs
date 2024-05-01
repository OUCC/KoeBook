using System.Xml.Serialization;

namespace KoeBook.Models;

[XmlRoot("Book")]
public class AiStory
{

    [XmlElement("Title", typeof(string), IsNullable = false)]
    public string Title { get; init; } = "";

    [XmlArray("Content", IsNullable = false)]
    [XmlArrayItem("Section", IsNullable = false)]
    [XmlArrayItem("Paragraph", IsNullable = false, NestingLevel = 1)]
    public Line[][] Lines { get; init; } = [];

    public record Line(
        [property: XmlElement("Text", typeof(Text), IsNullable = false), XmlElement("Ruby", typeof(Ruby), IsNullable = false)] InlineElement[] Inlines)
    {
        private Line() : this([]) { }

        public string GetText() => string.Concat(Inlines.Select(e => e.Html));

        public string GetScript() => string.Concat(Inlines.Select(e => e.Script));
    }

    public abstract record class InlineElement
    {
        public abstract string Html { get; }
        public abstract string Script { get; }
    }

    public record Text([property: XmlText] string InnerText) : InlineElement
    {
        private Text() : this("") { }

        public override string Html => InnerText;
        public override string Script => InnerText;
    }

    public record Ruby(
        [property: XmlElement("Rb", IsNullable = false)] string Rb,
        [property: XmlElement("Rt", IsNullable = false)] string Rt) : InlineElement
    {
        private Ruby() : this("", "") { }

        public override string Html => $"<ruby>{Rb}<rt>{Rt}</rt></ruby>";
        public override string Script => Rt;
    }
}
