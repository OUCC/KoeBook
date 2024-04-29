using KoeBook.Core.Contracts.Services;
using OpenAI;
using OpenAI.Interfaces;
using OpenAI.Managers;

namespace KoeBook.Core.Services;

public class MyOpenAiService(ISecretSettingsService secretSettingsService, IHttpClientFactory httpClientFactory) : IOpenAIService
{
    private readonly ISecretSettingsService _secretSettingsService = secretSettingsService;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private string? _apiKey;
    private OpenAIService? _openAiService;


    public IModelService Models => GetOpenAiService()?.Models!;

    public ICompletionService Completions => GetOpenAiService()?.Completions!;

    public IEmbeddingService Embeddings => GetOpenAiService()?.Embeddings!;

    public OpenAI.Interfaces.IFileService Files => GetOpenAiService()?.Files!;

    public IFineTuneService FineTunes => GetOpenAiService()?.FineTunes!;

    public IFineTuningJobService FineTuningJob => GetOpenAiService()?.FineTuningJob!;

    public IModerationService Moderation => GetOpenAiService()?.Moderation!;

    public IImageService Image => GetOpenAiService()?.Image!;

    public IEditService Edit => GetOpenAiService()?.Edit!;

    public IChatCompletionService ChatCompletion => GetOpenAiService()?.ChatCompletion!;

    public IAudioService Audio => GetOpenAiService()?.Audio!;

    public IBatchService Batch => GetOpenAiService()?.Batch!;

    public void SetDefaultModelId(string modelId)
    {
        GetOpenAiService()?.SetDefaultModelId(modelId);
    }

    private OpenAIService? GetOpenAiService()
    {
        if (_apiKey != _secretSettingsService.ApiKey)
        {
            if (string.IsNullOrEmpty(_secretSettingsService.ApiKey))
            {
                _apiKey = _secretSettingsService.ApiKey;
                _openAiService?.Dispose();
                _openAiService = null;
                return null;
            }
            var options = new OpenAiOptions
            {
                ApiKey = _secretSettingsService.ApiKey,
            };
            var openAiService = new OpenAIService(options, _httpClientFactory.CreateClient());

            _openAiService = openAiService;
            _apiKey = _secretSettingsService.ApiKey;
        }
        return _openAiService;
    }
}
