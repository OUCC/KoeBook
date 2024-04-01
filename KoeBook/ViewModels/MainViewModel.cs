using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KoeBook.Contracts.Services;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace KoeBook.ViewModels;

public sealed partial class MainViewModel : ObservableRecipient
{
    private readonly IGenerationTaskService _taskService;
    private readonly IDialogService _dialogService;
    private readonly ILocalSettingsService _localSettingsService;
    private readonly IScraperSelectorService _scraperSelectorService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EbookFileName))]
    [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
    private string? _ebookFilePath;

    public string EbookFileName => EbookFilePath is not null ? Path.GetFileName(EbookFilePath) : string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
    private string? _ebookUrl;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorTextVisibility))]
    [NotifyPropertyChangedFor(nameof(CanExecuteStartProcess))]
    private bool _ebookIsValid = true;

    public Visibility ErrorTextVisibility => EbookIsValid ? Visibility.Collapsed : Visibility.Visible;

    private bool CanExecuteStartProcess => EbookFilePath is not null || !string.IsNullOrEmpty(EbookUrl) && EbookIsValid;

    [ObservableProperty]
    private bool _skipEdit = true;

    const string SkipEditSettingsKey = "SkipEdit";

    public MainViewModel(IGenerationTaskService taskService, IDialogService dialogService, ILocalSettingsService localSettingsService, IScraperSelectorService scraperSelectorService, GenerationTaskRunnerService _)
    {
        _taskService = taskService;
        _dialogService = dialogService;
        _localSettingsService = localSettingsService;
        _scraperSelectorService = scraperSelectorService;

        InitializeAsync(); // 必ず最後に実行すること
    }

    private async void InitializeAsync()
    {
        SkipEdit = await _localSettingsService.ReadSettingAsync<bool?>(SkipEditSettingsKey) ?? true;
    }

    [RelayCommand]
    private async Task OpenEBookFileAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (!string.IsNullOrEmpty(EbookUrl))
            {
                var result = await _dialogService.ShowAsync("ローカルファイルを使用する場合、外部URLは無視されます。", cancellationToken);
                if (result == ContentDialogResult.Primary)
                    EbookUrl = null;
                else
                {
                    EbookFilePath = null;
                    return;
                }
            }

            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".epub");
            var window = App.MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync().AsTask(cancellationToken);
            if (file is not null)
                EbookFilePath = file.Path;
        }
        catch (OperationCanceledException)
        {
            return;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteStartProcess))]
    private async Task StartProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dialogService.ShowAsync(
                "EPubの生成", "読み上げの生成を開始しますか？この先の処理ではAPI料金がかかります。",
                "開始", cancellationToken);

            if (result != ContentDialogResult.Primary)
                return;
        }
        catch (OperationCanceledException)
        {
            return;
        }

        var source = EbookFilePath ?? EbookUrl!;
        _taskService.Register(new(Guid.NewGuid(),
            source,
            EbookFilePath is null ? SourceType.Url : SourceType.FilePath,
            SkipEdit));
        EbookFilePath = null;
        EbookUrl = null;
    }

    public async void BeforeTextChanging(TextBox _, TextBoxBeforeTextChangingEventArgs args)
    {
        if (EbookFilePath is null || string.IsNullOrEmpty(args.NewText))
            return;

        var result = await _dialogService.ShowAsync("ローカルファイルを無視します", "外部URLから使用する場合、ローカルファイルは無視されます。", default);
        if (result == ContentDialogResult.Primary)
        {
            EbookFilePath = null;
        }
        else
        {
            args.Cancel = true;
            EbookUrl = string.Empty;
        }
    }

    partial void OnSkipEditChanged(bool value)
    {
        CoreAsync(_localSettingsService, value);

        static async void CoreAsync(ILocalSettingsService settingsService, bool skipEdit)
        {
            await settingsService.SaveSettingAsync(SkipEditSettingsKey, skipEdit);
        }
    }

    partial void OnEbookUrlChanged(string? value)
    {
        EbookIsValid = string.IsNullOrEmpty(value) || _scraperSelectorService.IsMatchSites(value);
    }
}
