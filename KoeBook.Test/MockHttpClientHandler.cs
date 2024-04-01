using System.Net.Http.Headers;
using System.Text;

namespace KoeBook.Test;

internal class MockHttpClientHandler : HttpClientHandler
{
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.RequestUri?.GetLeftPart(UriPartial.Authority) switch
        {
            "https://ncode.syosetu.com" => ProcessRequest(request, cancellationToken),
            _ => base.Send(request, cancellationToken),
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.RequestUri?.GetLeftPart(UriPartial.Authority) switch
        {
            "https://ncode.syosetu.com" => Task.FromResult(ProcessRequest(request, cancellationToken)),
            _ => base.SendAsync(request, cancellationToken),
        };
    }

    private HttpResponseMessage ProcessRequest(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        if (requestMessage.Method != HttpMethod.Get || requestMessage.Headers.UserAgent.Count == 0)
            throw new NotSupportedException();
        var (filePath, mediaType, isString) = requestMessage.RequestUri?.ToString() switch
        {
            "https://ncode.syosetu.com/n0000aa" => ("./TestData/Naro/n0000aa.html", "text/html; charset=UTF-8", true),
            "https://ncode.syosetu.com/n0000aa/1" => ("./TestData/Naro/n0000aa/1.html", "text/html; charset=UTF-8", true),
            "https://ncode.syosetu.com/n0000aa/2" => ("./TestData/Naro/n0000aa/2.html", "text/html; charset=UTF-8", true),
            "https://ncode.syosetu.com/n0000aa/3" => ("./TestData/Naro/n0000aa/3.html", "text/html; charset=UTF-8", true),
            _ => throw new NotSupportedException()
        };

        return new HttpResponseMessage
        {
            Content = isString
                ? new StringContent(File.ReadAllText(filePath), Encoding.UTF8, MediaTypeHeaderValue.Parse(mediaType))
                : new ByteArrayContent(File.ReadAllBytes(filePath))
                    {
                        Headers =
                        {
                            ContentType = MediaTypeHeaderValue.Parse(mediaType),
                        }
                    }
        };
    }
}
