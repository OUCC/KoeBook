using System.Collections.Immutable;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoeBook.Contracts.Services;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Models;

namespace KoeBook.ViewModels;

public sealed partial class CreateStoryViewModel : ObservableObject
{
    private readonly IGenerationTaskService _generationTaskService;
    private readonly IDialogService _dialogService;
    private readonly IStoryCreatorService _storyCreatorService;

    public ImmutableArray<StoryGenre> Genres { get; } = [
        new("青春小説", "学校生活、友情、恋愛など、若者の成長物語"),
        new("ミステリー・サスペンス", "謎解きや犯罪、真相究明などのスリリングな物語"),
        new("SF", "未来、科学技術、宇宙などを題材にした物語"),
        new("ホラー", "恐怖や怪奇現象を扱った、読者の恐怖心をくすぐる物語"),
        new("ロマンス", "恋愛や結婚、人間関係などを扱った、胸キュンな物語"),
        new("コメディ", "ユーモアやギャグ、風刺などを交えた、読者を笑わせる物語"),
        new("歴史小説", "過去の出来事や人物を題材にした、歴史の背景が感じられる物語"),
        new("ノンフィクション・エッセイ", "実際の経験や知識、考えを綴った、リアルな物語"),
        new("詩集", "感情や思考、風景などを言葉で表現した、韻文形式の作品集"),
    ];

    [ObservableProperty]
    private StoryGenre _selectedGenre;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateStoryCommand))]
    private string _instruction = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartGenerateTaskCommand))]
    [NotifyPropertyChangedFor(nameof(AiStoryTitle))]
    private AiStory? _aiStory;

    public string AiStoryTitle => AiStory?.Title ?? "";

    public CreateStoryViewModel(IGenerationTaskService generationTaskService, IDialogService dialogService, IStoryCreatorService storyCreatorService)
    {
        _selectedGenre = Genres[0];
        _generationTaskService = generationTaskService;
        _dialogService = dialogService;
        _storyCreatorService = storyCreatorService;
    }

    public bool CanCreateStory => !string.IsNullOrWhiteSpace(Instruction);

    [RelayCommand(CanExecute = nameof(CanCreateStory))]
    private async Task OnCreateStoryAsync(CancellationToken cancellationToken)
    {
        using var sr = new StringReader(await _storyCreatorService.CreateStoryAsync(SelectedGenre, Instruction, cancellationToken));
        var serializer = new XmlSerializer(typeof(AiStory));
        try
        {
            AiStory = (AiStory?)serializer.Deserialize(sr);
        }
        catch (InvalidOperationException)
        {
            await _dialogService.ShowAsync("生成失敗", "AIによるコードの生成に失敗しました", "OK", cancellationToken);
        }
    }

    public bool CanStartGenerate => AiStory is not null;

    [RelayCommand(CanExecute = nameof(CanStartGenerate))]
    private void OnStartGenerateTask()
    {
        var aiStory = AiStory!;
        AiStory = null;
        _generationTaskService.Register(new GenerationTask(Guid.NewGuid(), aiStory, true));
    }
}

