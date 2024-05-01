using Claudia;

namespace KoeBook.Core.Contracts.Services;

public interface IClaudeService
{
    IMessages? Messages { get; }
}
