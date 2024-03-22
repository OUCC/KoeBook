using KoeBook.Core.Models;

namespace KoeBook.Core.Contracts.Services;

public interface ILlmAnalyzerService
{
    ValueTask<BookScripts> LlmAnalyzeScriptLinesAsync(BookProperties bookProperties, List<ScriptLine> scriptLines, CancellationToken cancellationToken);
}
