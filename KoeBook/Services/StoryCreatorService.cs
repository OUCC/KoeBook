using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;

namespace KoeBook.Services;

public class StoryCreatorService(ILlmStoryGeneratorService llmStoryGeneratorService) : IStoryCreatorService
{
    private readonly ILlmStoryGeneratorService _llmStoryGeneratorService = llmStoryGeneratorService;
    public async ValueTask<string> CreateStoryAsync(StoryGenre genre, string instruction, CancellationToken cancellationToken)
    {
        return await _llmStoryGeneratorService.GenerateStoryAsync(genre, instruction, cancellationToken);
    }
}
