using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using KoeBook.Epub.Models;
using KoeBook.Core;
using System.Xml.Serialization;
using KoeBook.Core.Utilities;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Epub.Services
{
    public partial class ScrapingClaudeStroyService(ISplitBraceService splitBraceService)
    {

        private readonly ISplitBraceService _splitBraceService = splitBraceService;
        public async ValueTask<EpubDocument> ScrapingAsync(string xmlText, string coverFillePath, string imgDirectory, Guid id, CancellationToken ct)
        {
            if (!FitXmlSchema(xmlText))
                throw new EbookException(ExceptionType.XmlSchemaUnfulfilled);

            var book = LoadBook(xmlText);

            var paragraphbuilder = new SplittedLineBuilder();
            var scriptLineBuilder = new SplittedLineBuilder();

            var document = new EpubDocument(book.Title, "", coverFillePath, id);
            document.Chapters.Add(new Chapter());
            foreach (var section in book.Content.Sections)
            {
                var docSection = new Section(section.Title ?? "");
                foreach (var paragraph in section.Paragraphs)
                {
                    foreach (var item in paragraph.Texts)
                    {
                        switch (item)
                        {
                            case Text text:
                                paragraphbuilder.Append(text.InnerText);
                                scriptLineBuilder.Append(text.InnerText);
                                break;
                            case Ruby ruby:
                                paragraphbuilder.Append($"<ruby>{ruby.Rb}<rp>(</rp><rt>{ruby.Rt}</rt><rp>)</rp></ruby>");
                                scriptLineBuilder.Append($"{ruby.Rt}");
                                break;
                            default:
                                throw new EbookException(ExceptionType.XmlSchemaUnfulfilled);
                        }
                    }

                    foreach ((var textSplit, var scriptSplit) in
                        _splitBraceService.SplitBrace(paragraphbuilder.ToLinesAndClear()).Zip(
                            _splitBraceService.SplitBrace(scriptLineBuilder.ToLinesAndClear())
                        )
                    )
                    {
                        docSection.Elements.Add(new Paragraph() { Text = textSplit, ScriptLine = new ScriptLine(scriptSplit, "", "") });
                    }
                }
                document.Chapters.Single().Sections.Add(docSection);
            }
            return document;
        }

        internal bool FitXmlSchema(string xmlText)
        {
            // スキーマファイルのパスを入れる。
            using var xsdFs = new FileStream(@"", FileMode.Open);
            XmlSchemaSet schema = new XmlSchemaSet();
            schema.Add("", XmlReader.Create(xsdFs));

            XDocument xml = XDocument.Parse(xmlText);

            bool errorExist = false;
            xml.Validate(schema, (o, e) => errorExist = true);

            return !errorExist;
        }

        internal Book LoadBook(string xmlText)
        {
            using var xmlStringReader = new StringReader(xmlText);
            var serializer = new XmlSerializer(typeof(Book));
            return (Book)serializer.Deserialize(xmlStringReader)
                ?? throw new EbookException(ExceptionType.XmlSchemaUnfulfilled);
        }
    }
}
