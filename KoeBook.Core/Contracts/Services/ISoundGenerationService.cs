using KoeBook.Core.Models;
using KoeBook.Epub.Models;

namespace KoeBook.Core.Contracts.Services;

public interface ISoundGenerationService
{
    /// <summary>
    /// 1文の音声を生成します
    /// </summary>
    /// <param name="scriptLine"></param>
    /// <param name="bookOptions"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<Audio> GenerateLineSoundAsync(ScriptLine scriptLine, BookOptions bookOptions, CancellationToken cancellationToken);
}
