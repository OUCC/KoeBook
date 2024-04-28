using System.Collections.Generic;
using System.Xml.Serialization;

namespace KoeBook.Epub.Models;

[XmlRoot("Book")]
public record Book
{
    [XmlElement("Title", typeof(string))]
    public string Title;

    [XmlElement("Content")]
    public Content Content;
}

public record Content
{
    [XmlElement("Section")]
    public List<XmlSection> Sections;
}

public record XmlSection
{
    [XmlElement("Title")]
    public string? Title;

    [XmlElement("Paragraph")]
    public List<XmlParagraph> Paragraphs;
}

public record ParagraphPart { }

public record XmlParagraph
{
    [XmlElement("Text", typeof(Text))]
    [XmlElement("Ruby", typeof(Ruby))]
    public List<ParagraphPart> Texts;
}

public record Text : ParagraphPart
{
    [XmlElement("Text")]
    public string InnerText;
}


public record Ruby : ParagraphPart
{
    [XmlElement("Rb")]
    public string Rb;

    [XmlElement("Rt")]
    public string Rt;
}
