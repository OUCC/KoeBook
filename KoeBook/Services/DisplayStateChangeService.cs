using KoeBook.Contracts.Services;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Models;
using Microsoft.UI.Dispatching;

namespace KoeBook.Services;

internal class DisplayStateChangeService(IGenerationTaskService taskService) : IDisplayStateChangeService
{
    private readonly IGenerationTaskService _taskService = taskService;

    public void UpdateProgress(BookProperties bookProperties, int progress, int maximum)
    {
        var taskService = _taskService; // thisをキャプチャしないようにする
        _ = App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            var task = taskService.GetProcessingTask(bookProperties.Id);
            task.MaximumProgress = maximum;
            task.Progress = progress;
        });
    }

    public void UpdateState(BookProperties bookProperties, GenerationState state)
    {
        var taskService = _taskService; // thisをキャプチャしないようにする
        _ = App.MainWindow.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            var task = taskService.GetProcessingTask(bookProperties.Id);
            if(task.State < state)
                task.State = state;
        });
    }

    public void UpdateTitle(BookProperties bookProperties, string title)
    {
        var taskService = _taskService; // thisをキャプチャしないようにする
        _ = App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            taskService.GetProcessingTask(bookProperties.Id).Title = title;
        });
    }
}
