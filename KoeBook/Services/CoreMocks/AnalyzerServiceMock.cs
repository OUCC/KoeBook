using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Helpers;
using KoeBook.Core.Models;
using KoeBook.Epub;
using KoeBook.Epub.Models;
using static KoeBook.Core.Helpers.IDisplayStateChangeEx;

namespace KoeBook.Services.CoreMocks;

public class AnalyzerServiceMock(IDisplayStateChangeService stateService) : IAnalyzerService
{
    private readonly IDisplayStateChangeService _stateService = stateService;

    public async ValueTask<BookScripts> AnalyzeAsync(BookProperties bookProperties, string tempDirectory, string coverFilePath, CancellationToken cancellationToken)
    {
        DisplayStateChanging stateChanging;
        if (bookProperties.SourceType == SourceType.Url)
        {
            stateChanging = _stateService.ResetProgress(bookProperties, GenerationState.Downloading, 300);
            await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
            stateChanging.UpdateProgress(30);
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            stateChanging.UpdateProgress(100);
            await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
            stateChanging.UpdateProgress(300);
        }

        stateChanging = _stateService.ResetProgress(bookProperties, GenerationState.Analyzing, 400);
        await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
        stateChanging.UpdateProgress(240);
        await Task.Delay(3000, cancellationToken).ConfigureAwait(false);
        stateChanging.UpdateProgress(400);

        var characterMapping = new Dictionary<string, string>()
        {
            ["Hoge"] = "青年1",
            ["Fuga"] = "青年2",
            ["Narration"] = "ナレーション",
        };
        return new(bookProperties, new(characterMapping))
        {
            ScriptLines = [
                new("読み上げテキスト1", "Hoge", "Angry"),
                new("読み上げテキスト2", "Fuga", "Sad"),
                new("読み上げテキスト3", "Narration", "Narration"),
            ],
        };
    }
}
