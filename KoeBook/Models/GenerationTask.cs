using CommunityToolkit.Mvvm.ComponentModel;
using FastEnumUtility;
using KoeBook.Core.Models;

namespace KoeBook.Models;

public partial class GenerationTask : ObservableObject
{
    public GenerationTask(Guid id, string source, SourceType sourceType, bool skipEdit)
    {
        if (sourceType != SourceType.FilePath && sourceType != SourceType.Url)
            throw new ArgumentException($"{nameof(sourceType)}は{nameof(SourceType.FilePath)}か{nameof(SourceType.Url)}である必要があります。");
        Id = id;
        _rawSource = source;
        SourceType = sourceType;
        _skipEdit = skipEdit;
        _title   = sourceType == SourceType.FilePath ? Path.GetFileName(source) : source;
    }

    public GenerationTask(Guid id, AiStory aiStory, bool skipEdit)
    {
        Id = id;
        _rawSource = aiStory;
        SourceType = SourceType.AiStory;
        _skipEdit= skipEdit;
        _title = aiStory.Title;
    }

    public BookProperties ToBookProperties()
    {
        return SourceType == SourceType.AiStory
            ? new BookProperties(Id, (AiStory)_rawSource)
            : new BookProperties(Id, Source, SourceType);
    }

    public Guid Id { get; }

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public CancellationToken CancellationToken => CancellationTokenSource.Token;

    public string Source => _rawSource is string uri ? uri : "AI生成";

    private readonly object _rawSource;

    public SourceType SourceType { get; }

    public string SourceDescription => SourceType switch
    {
        SourceType.Url => "URL",
        SourceType.FilePath => "ファイルパス",
        SourceType.AiStory => "AI生成",
        _ => string.Empty,
    };

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private int _progress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private int _maximumProgress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StateText))]
    [NotifyPropertyChangedFor(nameof(SkipEditChangable))]
    [NotifyPropertyChangedFor(nameof(Editable))]
    private GenerationState _state;

    public string StateText => State.GetEnumMemberValue()!;

    public string ProgressText => $"{Progress}/{MaximumProgress}";

    public bool SkipEdit
    {
        get => _skipEdit;
        set
        {
            if (_skipEdit != value && SkipEditChangable)
            {
                OnPropertyChanging(nameof(SkipEdit));
                _skipEdit = value;
                OnPropertyChanged(nameof(SkipEdit));
            }
        }
    }
    private bool _skipEdit;

    public bool SkipEditChangable => State < GenerationState.Editting;

    public bool Editable => State == GenerationState.Editting;

    [ObservableProperty]
    private BookScripts? _bookScripts;

    partial void OnMaximumProgressChanging(int value)
    {
        if (value < Progress)
            Progress = value;
    }
}
