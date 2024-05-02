using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Core.Models.StyleBertVits;

namespace KoeBook.Core.Services;

public class SoundGenerationSelectorService(IStyleBertVitsClientService styleBertVitsClientService) : ISoundGenerationSelectorService
{
    private readonly IStyleBertVitsClientService _styleBertVitsClientService = styleBertVitsClientService;

    public IReadOnlyList<SoundModel> Models { get; private set; } = [];

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var models = await _styleBertVitsClientService
                .GetFromJsonAsync<Dictionary<string, ModelInfo>>("/models/info", ExceptionType.InitializeFailed, cancellationToken)
                .ConfigureAwait(false);

            Models = models.Select(kvp => new SoundModel(kvp.Key, kvp.Value.FirstSpk.Replace(" ",""), kvp.Value.Styles)).ToArray();
        }
        catch (EbookException e) when (e.ExceptionType == ExceptionType.UnknownStyleBertVitsRoot) { }
    }
}
