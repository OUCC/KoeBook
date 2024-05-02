using Microsoft.UI.Xaml.Controls;

namespace KoeBook.Contracts.Services;

public interface IDialogService
{
    Task<ContentDialogResult> ShowAsync(
        string title,
        object content,
        string primaryText,
        string? closeText,
        ContentDialogButton defaultButton,
        CancellationToken cancellationToken);

    Task<ContentDialogResult> ShowAsync(
        string title,
        string content,
        string primaryText,
        string? closeText,
        ContentDialogButton defaultButton,
        CancellationToken cancellationToken);

    Task<ContentDialogResult> ShowInfoAsync(
        string title,
        string content,
        string primaryText,
        CancellationToken cancellationToken);
}

public static class DialogServiceEx
{
    public static Task<ContentDialogResult> ShowAsync(
        this IDialogService dialogService,
        string title,
        string content,
        string primaryText,
        string closeText,
        CancellationToken cancellationToken)
        => dialogService.ShowAsync(title, content, primaryText, closeText, ContentDialogButton.Primary, cancellationToken);

    public static Task<ContentDialogResult> ShowAsync(
        this IDialogService dialogService,
        string title,
        string content,
        string primaryText,
        CancellationToken cancellationToken)
        => dialogService.ShowAsync(title, content, primaryText, "キャンセル", cancellationToken);

    public static Task<ContentDialogResult> ShowAsync(
        this IDialogService dialogService,
        string title,
        string content,
        CancellationToken cancellationToken)
        => dialogService.ShowAsync(title, content, "OK", cancellationToken);

    public static Task<ContentDialogResult> ShowAsync(
        this IDialogService dialogService,
        string content,
        CancellationToken cancellationToken)
        => dialogService.ShowAsync("KoeBookからのお知らせ", content, cancellationToken);
}
