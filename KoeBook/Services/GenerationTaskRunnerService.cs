using FastEnumUtility;
using KoeBook.Contracts.Services;
using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using KoeBook.Models;
using Windows.Storage;

namespace KoeBook.Services;

public class GenerationTaskRunnerService
{
    private readonly IGenerationTaskService _taskService;
    private readonly IAnalyzerService _analyzerService;
    private readonly IEpubGenerateService _epubGenService;
    private readonly IDialogService _dialogService;
    private readonly string _tempFolder = ApplicationData.Current.TemporaryFolder.Path;

    public GenerationTaskRunnerService(
        IGenerationTaskService taskService,
        IAnalyzerService analyzerService,
        IEpubGenerateService epubGenService,
        IDialogService dialogService)
    {
        _taskService = taskService;
        _taskService.OnTasksChanged += TasksChanged;
        _analyzerService = analyzerService;
        _epubGenService = epubGenService;
        _dialogService = dialogService;
    }

    private async void TasksChanged(GenerationTask task, ChangedEvents changedEvents)
    {
        switch (changedEvents)
        {
            case ChangedEvents.Registered:
                {
                    await RunAsync(task);
                    break;
                }
            case ChangedEvents.Unregistered:
                {
                    task.CancellationTokenSource.Cancel();
                    break;
                }
        }
    }

    private async ValueTask RunAsync(GenerationTask task)
    {
        if (task.CancellationToken.IsCancellationRequested || task.State == GenerationState.Failed)
            return;

        await RunAsyncCore(task, true);
        if (task.SkipEdit)
            await RunAsyncCore(task, false);
    }

    public async void RunGenerateEpubAsync(GenerationTask task)
    {
        if (task.CancellationToken.IsCancellationRequested || task.State == GenerationState.Failed || task.BookScripts is null)
            return;

        await RunAsyncCore(task, false);
    }

    private async ValueTask RunAsyncCore(GenerationTask task, bool firstStep)
    {
        var tempDirectory = Path.Combine(_tempFolder, task.Id.ToString());
        try
        {
            if (firstStep)
            {
                var scripts = await _analyzerService.AnalyzeAsync(new(task.Id, task.Source, task.SourceType), tempDirectory, task.CancellationToken);
                task.BookScripts = scripts;
                task.State = GenerationState.Editting;
                task.Progress = 0;
                task.MaximumProgress = 0;
            }
            else if (task.BookScripts is not null)
            {
                var resultPath = await _epubGenService.GenerateEpubAsync(task.BookScripts, tempDirectory, task.CancellationToken);
                task.State = GenerationState.Completed;
                task.Progress = 1;
                task.MaximumProgress = 1;
                var fileName = Path.GetFileName(resultPath);
                var resultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "KoeBook");
                if (!Directory.Exists(resultDirectory))
                    Directory.CreateDirectory(resultDirectory);
                File.Move(resultPath, Path.Combine(resultDirectory, fileName), true);
            }
            else
                throw new InvalidOperationException();
        }
        catch (OperationCanceledException)
        {
            task.State = GenerationState.Failed;
        }
        catch (EbookException e)
        {
            task.State = GenerationState.Failed;
            await _dialogService.ShowInfoAsync("生成失敗", e.ExceptionType.GetEnumMemberValue()!, "OK", default);
        }
        catch (Exception e)
        {
            task.State = GenerationState.Failed;
            await _dialogService.ShowInfoAsync("生成失敗", $"不明なエラーが発生しました。\n{e.Message}", "OK", default);
        }
    }

    public void StopTask(GenerationTask task)
    {
        task.CancellationTokenSource.Cancel();
        if (task.State != GenerationState.Completed)
        {
            task.State = GenerationState.Failed;
            task.Progress = 0;
            task.MaximumProgress = 0;
        }
        _taskService.Unregister(task.Id);
    }
}
