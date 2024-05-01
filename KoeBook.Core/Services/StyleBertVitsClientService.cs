using System;
using System.Net.Http.Json;
using KoeBook.Core.Contracts.Services;

namespace KoeBook.Core.Services;

public class StyleBertVitsClientService(IHttpClientFactory httpClientFactory, IApiRootSelectorService apiRootSelectorService) : IStyleBertVitsClientService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IApiRootSelectorService _apiRootSelectorService = apiRootSelectorService;

    public async ValueTask<T> GetFromJsonAsync<T>(string path, ExceptionType exceptionType, CancellationToken cancellationToken)
    {
        var content = await GetAsync(path, exceptionType, cancellationToken).ConfigureAwait(false);
        return await content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false)
            ?? throw new EbookException(exceptionType);
    }

    public async ValueTask<byte[]> GetAsByteArrayAsync(string path, ExceptionType exceptionType, CancellationToken cancellationToken)
    {
        var content = await GetAsync(path, exceptionType, cancellationToken).ConfigureAwait(false);
        return await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<HttpContent> GetAsync(string path, ExceptionType exceptionType, CancellationToken cancellationToken)
    {
        try
        {
            var root = _apiRootSelectorService.StyleBertVitsRoot;
            if (string.IsNullOrEmpty(root))
                throw new EbookException(ExceptionType.UnknownStyleBertVitsRoot);
            var baseUri = new Uri(root);
            var requestUri = new Uri(baseUri, path);
            var response = await _httpClientFactory.CreateClient()
                .GetAsync(requestUri, cancellationToken)
                .ConfigureAwait(false) ?? throw new EbookException(exceptionType);

            if (!response.IsSuccessStatusCode)
                throw new EbookException(exceptionType);
            return response.Content;
        }
        catch (OperationCanceledException) { throw; }
        catch (EbookException) { throw; }
        catch (Exception e)
        {
            throw new EbookException(exceptionType, innerException: e);
        }
    }
}
