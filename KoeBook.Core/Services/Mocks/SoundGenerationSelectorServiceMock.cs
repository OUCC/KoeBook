using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Core.Services.Mocks;

public class SoundGenerationSelectorServiceMock : ISoundGenerationSelectorService
{
    public IReadOnlyList<SoundModel> Models { get; private set; } = [];

    public async ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        Models = [
            new SoundModel("0", "MaleNarrator", ["narration"]),
            new SoundModel("1", "ElementarySchoolBoy", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("2", "MiddleHighSchoolBoy", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("3", "AdultMan", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("4", "ElderlyMan", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("5", "FemaleNarrator", ["narration"]),
            new SoundModel("6", "ElementarySchoolGirl", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("7", "MiddleHighSchoolGirl", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("8", "AdultWoman", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"]),
            new SoundModel("9", "ElderlyWoman", ["neutral", "laughing", "happy", "sad", "cry", "surprised", "angry"])
        ];
    }
}
