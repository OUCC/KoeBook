using KoeBook.Contracts.Services;
using Microsoft.UI.Xaml.Controls;

namespace KoeBook.Components.Dialog;

public class DialogService : IDialogService
{
    private readonly IThemeSelectorService _themeSelectorService;

    public DialogService(IThemeSelectorService themeSelectorService)
    {
        _themeSelectorService = themeSelectorService;
    }

    public Task<ContentDialogResult> ShowAsync(
        string title,
        object content,
        string primaryText,
        string? closeText,
        ContentDialogButton defaultButton,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contentDialog = new SharedContentDialog(title, primaryText, closeText, defaultButton)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            Content = content,
            RequestedTheme = _themeSelectorService.Theme
        };

        return contentDialog.ShowAsync().AsTask(cancellationToken);
    }

    public Task<ContentDialogResult> ShowAsync(
        string title,
        string content,
        string primaryText,
        string? closeText,
        ContentDialogButton defaultButton,
        CancellationToken cancellationToken)
    {
        return ShowAsync(title, new DialogContentControl(content), primaryText, closeText, defaultButton, cancellationToken);
    }

    public Task<ContentDialogResult> ShowInfoAsync(string title, string content, string primaryText, CancellationToken cancellationToken)
    {
        var dialog = new SharedContentDialog(title, primaryText, null, ContentDialogButton.Primary)
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            Content = new DialogContentControl(content),
            RequestedTheme = _themeSelectorService.Theme
        };
        return dialog.ShowAsync().AsTask(cancellationToken);
    }
}
