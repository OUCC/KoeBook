using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Models;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace KoeBook.Epub.Services;
public class EpubCreateService(IFileExtensionService fileExtensionService) : IEpubCreateService
{
    private readonly IFileExtensionService _fileExtensionService = fileExtensionService;

    internal const string ContainerXml = """
         <?xml version="1.0" encoding="UTF-8"?>
         <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
             <rootfiles>
                 <rootfile full-path="OEBPS/book.opf" media-type="application/oebps-package+xml" />
             </rootfiles>
         </container>
         """;

    public async ValueTask<bool> TryCreateEpubAsync(EpubDocument epubDocument, string tmpDirectory, CancellationToken ct)
    {
        if (!File.Exists(epubDocument.CoverFilePath))
        {
            throw new FileNotFoundException("指定されたカバーファイルが存在しません", epubDocument.CoverFilePath);
        }
        try
        {
            using var fs = File.Create(Path.Combine(tmpDirectory, $"{epubDocument.Id}.epub"));
            using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

            var mimeTypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var mimeTypeStream = new StreamWriter(mimeTypeEntry.Open()))
            {
                await mimeTypeStream.WriteAsync("application/epub+zip").ConfigureAwait(false);
                await mimeTypeStream.FlushAsync(ct).ConfigureAwait(false);
            }

            var containerEntry = archive.CreateEntry("META-INF/container.xml");
            using (var containerStream = new StreamWriter(containerEntry.Open()))
            {
                await containerStream.WriteLineAsync(ContainerXml).ConfigureAwait(false);
                await containerStream.FlushAsync(ct).ConfigureAwait(false);
            }

            archive.CreateEntryFromFile(epubDocument.CoverFilePath, $"OEBPS/{Path.GetFileName(epubDocument.CoverFilePath)}");

            var cssEntry = archive.CreateEntry("OEBPS/style.css");
            using (var cssStream = new StreamWriter(cssEntry.Open()))
            {
                await cssStream.WriteLineAsync(CreateCssText(epubDocument)).ConfigureAwait(false);
                await cssStream.FlushAsync(ct).ConfigureAwait(false);
            }

            var navEntry = archive.CreateEntry("OEBPS/nav.xhtml");
            using (var navStream = new StreamWriter(navEntry.Open()))
            {
                await navStream.WriteLineAsync(CreateNavXhtml(epubDocument)).ConfigureAwait(false);
                await navStream.FlushAsync(ct).ConfigureAwait(false);
            }

            var opfEntry = archive.CreateEntry("OEBPS/book.opf");
            using (var opfStream = new StreamWriter(opfEntry.Open()))
            {
                await opfStream.WriteLineAsync(CreateOpf(epubDocument)).ConfigureAwait(false);
                await opfStream.FlushAsync(ct).ConfigureAwait(false);
            }

            for (var i = 0; i < epubDocument.Chapters.Count; i++)
            {
                for (var j = 0; j < epubDocument.Chapters[i].Sections.Count; j++)
                {
                    var sectionXhtmlEntry = archive.CreateEntry($"OEBPS/{epubDocument.Chapters[i].Sections[j].Id}.xhtml");
                    using (var sectionXhtmlStream = new StreamWriter(sectionXhtmlEntry.Open()))
                    {
                        await sectionXhtmlStream.WriteLineAsync(CreateSectionXhtml(epubDocument.Chapters[i].Sections[j])).ConfigureAwait(false);
                        await sectionXhtmlStream.FlushAsync(ct).ConfigureAwait(false);
                    }
                    var sectionSmilEntry = archive.CreateEntry($"OEBPS/{epubDocument.Chapters[i].Sections[j].Id}_audio.smil");
                    using (var sectionSmilStream = new StreamWriter(sectionSmilEntry.Open()))
                    {
                        await sectionSmilStream.WriteLineAsync(CreateSectionSmil(epubDocument.Chapters[i].Sections[j])).ConfigureAwait(false);
                        await sectionSmilStream.FlushAsync(ct).ConfigureAwait(false);
                    }
                    for (var k = 0; k < epubDocument.Chapters[i].Sections[j].Elements.Count; k++)
                    {
                        var element = epubDocument.Chapters[i].Sections[j].Elements[k];
                        if (element is Paragraph para && para.Audio != null)
                        {
                            var audioEntry = archive.CreateEntry($"OEBPS/{epubDocument.Chapters[i].Sections[j].Id}_p{k}.mp3");
                            using var audioStream = para.Audio.GetStream();
                            using var audioEntryStream = audioEntry.Open();
                            await audioStream.CopyToAsync(audioEntryStream, ct).ConfigureAwait(false);
                            await audioEntryStream.FlushAsync(ct).ConfigureAwait(false);
                        }
                        else if (element is Picture pic && File.Exists(pic.PictureFilePath))
                        {
                            archive.CreateEntryFromFile(pic.PictureFilePath, $"OEBPS/{epubDocument.Chapters[i].Sections[j].Id}_p{k}{Path.GetExtension(pic.PictureFilePath)}");
                        }
                    }
                }
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    internal static string CreateNavXhtml(EpubDocument epubDocument)
    {
        var builder = new StringBuilder($"""
            <?xml version="1.0" encoding="UTF-8"?>
            <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
                <head>
                    <meta charset="UTF-8"/>
                    <title>{epubDocument.Title}</title>
                </head>
                <body>
                    <nav epub:type="toc" id="toc">
                        <ol>

            """);
        if (epubDocument.Chapters.Count == 1 && epubDocument.Chapters[0].Title == null)
        {
            for (var i = 0; i < epubDocument.Chapters[0].Sections.Count; i++)
            {
                builder.AppendLine($"""
                                <li>
                                    <a href="{epubDocument.Chapters[0].Sections[i].Id}.xhtml#s_{epubDocument.Chapters[0].Sections[i].Id}_p0">{epubDocument.Chapters[0].Sections[i].Title}</a>
                                </li>
                """);
            }
        }
        else
        {
            for (var i = 0; i < epubDocument.Chapters.Count; i++)
            {
                builder.AppendLine($"""
                                    <li>
                                        <span>{epubDocument.Chapters[i].Title}</span>
                                        <ol>
                    """);
                for (var j = 0; j < epubDocument.Chapters[i].Sections.Count; j++)
                {
                    builder.AppendLine($"""
                                                <li>
                                                    <a href="{epubDocument.Chapters[i].Sections[j].Id}.xhtml#s_{epubDocument.Chapters[i].Sections[j].Id}_p0">{epubDocument.Chapters[i].Sections[j].Title}</a>
                                                </li>
                        """);
                }
                builder.AppendLine($"""
                                        </ol>
                                    </li>
                    """);
            }
        }
        builder.AppendLine($"""
                        </ol>
                    </nav>
                </body>
            </html>
            """);
        return builder.ToString();
    }

    internal static string CreateCssText(EpubDocument epubDocument)
    {
        var builder = new StringBuilder();
        foreach (var cssClass in epubDocument.CssClasses)
        {
            builder.AppendLine(cssClass.Text);
        }
        return builder.ToString();
    }

    internal string CreateOpf(EpubDocument epubDocument)
    {
        var builder = new StringBuilder($"""
            <package unique-identifier="pub-id" version="3.0" xmlns="http://www.idpf.org/2007/opf">
                <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:title  id="title">{epubDocument.Title}</dc:title>
                    <dc:creator id="creator">{epubDocument.Author}</dc:creator>
                    <meta refines="#creator" property="role" scheme="marc:relators">aut</meta>
                    <dc:identifier id="pub-id">urn:uuid:{Guid.NewGuid()}</dc:identifier>
                    <dc:language>ja</dc:language>
                    <dc:date>{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}</dc:date>
                    <meta property="dcterms:modified">{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}</meta>
                    <meta property="media:active-class">-epub-media-overlay-active</meta>
                    <meta property="media:playback-active-class">-epub-media-overlay-unactive</meta>

            """);

        var totalTime = TimeSpan.Zero;
        for (var i = 0; i < epubDocument.Chapters.Count; i++)
        {
            for (var j = 0; j < epubDocument.Chapters[i].Sections.Count; j++)
            {
                var time = epubDocument.Chapters[i].Sections[j].GetTotalTime();
                totalTime += time;
                builder.AppendLine($"""
                                <meta property="media:duration" refines="#smil_{i}_{j}">{time}</meta>
                        """);
            }
        }
        builder.AppendLine($"""
                    <meta property="media:duration">{totalTime}</meta>
                </metadata>
                <manifest>
                    <item id="css" media-type="text/css" href="style.css"/>
                    <item id="cover" href="{Path.GetFileName(epubDocument.CoverFilePath)}" properties="cover-image" media-type="{_fileExtensionService.GetImagesMediaType(epubDocument.CoverFilePath)}" />
                    <item id="nav" href="nav.xhtml" properties="nav" media-type="application/xhtml+xml" />
            """);

        for (var i = 0; i < epubDocument.Chapters.Count; i++)
        {
            for (var j = 0; j < epubDocument.Chapters[i].Sections.Count; j++)
            {
                builder.AppendLine($"""
                                <item id="section_{i}_{j}" href="{epubDocument.Chapters[i].Sections[j].Id}.xhtml" media-type="application/xhtml+xml" media-overlay="smil_{i}_{j}" />
                                <item id="smil_{i}_{j}" href="{epubDocument.Chapters[i].Sections[j].Id}_audio.smil" media-type="application/smil+xml" />
                        """);
                for (var k = 0; k < epubDocument.Chapters[i].Sections[j].Elements.Count; k++)
                {
                    var element = epubDocument.Chapters[i].Sections[j].Elements[k];
                    if (element is Paragraph para && para.Audio != null)
                    {
                        builder.AppendLine(@$"        <item id=""audio_{i}_{j}_{k}"" href=""{epubDocument.Chapters[i].Sections[j].Id}_p{k}.mp3"" media-type=""audio/mpeg"" />");
                    }
                    else if (element is Picture pic && File.Exists(pic.PictureFilePath))
                    {
                        builder.AppendLine(@$"        <item id=""img_{i}_{j}_{k}"" href=""{epubDocument.Chapters[i].Sections[j].Id}_p{k}{Path.GetExtension(pic.PictureFilePath)}"" media-type=""{_fileExtensionService.GetImagesMediaType(pic.PictureFilePath)}"" />");
                    }
                }
            }
        }

        builder.AppendLine($"""
                </manifest>
                <spine page-progression-direction="ltr">
            """);

        for (var i = 0; i < epubDocument.Chapters.Count; i++)
        {
            for (var j = 0; j < epubDocument.Chapters[i].Sections.Count; j++)
            {
                builder.AppendLine($"""
                                <itemref idref="section_{i}_{j}" id="itemref_{i}_{j}" />
                        """);
            }
        }

        builder.AppendLine($"""
                </spine>
            </package>
            """);
        return builder.ToString();
    }


    internal static string CreateSectionXhtml(Section section)
    {
        var builder = new StringBuilder($"""
            <?xml version="1.0" encoding="UTF-8"?>
            <html xmlns="http://www.w3.org/1999/xhtml" xml:lang="ja" lang="ja">
                <head>  
                    <link rel="stylesheet" type="text/css" media="all" href="style.css"/>
                    <title>{section.Title}</title> 
                </head> 
                <body>
            """);

        for (var i = 0; i < section.Elements.Count; i++)
        {
            if (section.Elements[i] is Paragraph para)
            {
                builder.AppendLine($"""
                            <p id="s_{section.Id}_p{i}" {(para.ClassName != null ? $"class=\"{para.ClassName}\"" : "")}>
                                {para.Text}
                            </p>
                    """);
            }
            else if (section.Elements[i] is Picture pic && File.Exists(pic.PictureFilePath))
            {
                builder.AppendLine($"""
                            <p id="s_{section.Id}_p{i}" {(pic.ClassName != null ? $"class=\"{pic.ClassName}\"" : "")}>
                                <img src="{Path.GetFileName(pic.PictureFilePath)}"
                            </p>
                    """);
            }
        }

        builder.AppendLine("""
                </body>
            </html>
            """);
        return builder.ToString();
    }

    internal static string CreateSectionSmil(Section section)
    {
        var builder = new StringBuilder($"""
            <?xml version="1.0" encoding="UTF-8"?>
            <smil xmlns="http://www.w3.org/ns/SMIL" version="3.0">
                <body>
            """);

        for (var i = 0; i < section.Elements.Count; i++)
        {
            if (section.Elements[i] is Paragraph para && para.Audio != null)
            {
                builder.AppendLine($"""
                    <par id="s_{section.Id}_p{i}_audio" {(para.ClassName != null ? $"class=\"{para.ClassName}\"" : "")}>
                        <text src="{section.Id}.xhtml#s_{section.Id}_p{i}" />
                        <audio clipBegin="0s" clipEnd="{para.Audio?.TotalTime}" src="{section.Id}_p{i}.mp3"/>
                    </par>
            """);
            }
        }

        builder.AppendLine("""
                </body>
            </smil>
            """);
        return builder.ToString();
    }
}
