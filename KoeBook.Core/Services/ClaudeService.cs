using Claudia;
using KoeBook.Core.Contracts.Services;

namespace KoeBook.Core.Services;

public class ClaudeService(ISecretSettingsService secretSettingsService) : IClaudeService
{
    private readonly ISecretSettingsService _secretSettingsService = secretSettingsService;

    private string? _apiKey;
    private Anthropic? _anthropic;

    public IMessages? Messages => GetAnthropic()?.Messages;


    private Anthropic? GetAnthropic()
    {
        if (_apiKey != _secretSettingsService.ApiKey)
        {
            if (string.IsNullOrEmpty(_secretSettingsService.ApiKey))
            {
                _apiKey = _secretSettingsService.ApiKey;
                _anthropic?.Dispose();
                _anthropic = null;
                return null;
            }

            _anthropic = new Anthropic { ApiKey = _secretSettingsService.ApiKey };
            _apiKey = _secretSettingsService.ApiKey;
        }
        return _anthropic;
    }
}
