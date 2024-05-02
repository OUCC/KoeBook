using System.Web;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Core.Services;

public class SoundGenerationService(
    IStyleBertVitsClientService styleBertVitsClientService,
    ISoundGenerationSelectorService soundGenerationSelectorService) : ISoundGenerationService
{
    private readonly IStyleBertVitsClientService _styleBertVitsClientService = styleBertVitsClientService;
    private readonly ISoundGenerationSelectorService _soundGenerationSelectorService = soundGenerationSelectorService;

    public async ValueTask<byte[]> GenerateLineSoundAsync(ScriptLine scriptLine, BookOptions bookOptions, CancellationToken cancellationToken)
    {
        var model = bookOptions.CharacterMapping[scriptLine.Character];
        var soundModel = _soundGenerationSelectorService.Models.FirstOrDefault(m => m.Name == model)
            ?? throw new EbookException(ExceptionType.SoundGenerationFailed);
        var style = soundModel.Styles.Contains(scriptLine.Style) ? scriptLine.Style : soundModel.Styles[0];
        var queryCollection = HttpUtility.ParseQueryString(string.Empty);
        queryCollection.Add("text", scriptLine.Text);
        queryCollection.Add("model_id", soundModel.Id);
        queryCollection.Add("style", style);
        return await _styleBertVitsClientService
            .GetAsByteArrayAsync($"/voice?{queryCollection}", ExceptionType.SoundGenerationFailed, cancellationToken).ConfigureAwait(false);
    }
}
