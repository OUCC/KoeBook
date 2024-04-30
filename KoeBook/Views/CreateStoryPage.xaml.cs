using System.Diagnostics;
using KoeBook.Models;
using KoeBook.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace KoeBook.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CreateStoryPage : Page
{
    public static readonly Guid Id = Guid.NewGuid();

    public CreateStoryViewModel ViewModel { get; }

    public CreateStoryPage()
    {
        ViewModel = App.GetService<CreateStoryViewModel>();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        InitializeComponent();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ViewModel.AiStory))
            return;

        StoryContnent.Inlines.Clear();
        if (ViewModel.AiStory is null)
            return;

        foreach (var inline in ViewModel.AiStory.Lines
            .SelectMany(l =>
                l.SelectMany(l =>
                    l.Inlines.Select(Inline (inline) => inline switch
                    {
                        AiStory.Text text => new Run() { Text = text.InnerText },
                        AiStory.Ruby ruby => new Run() { Text = ruby.Rt },
                        _ => throw new UnreachableException(),
                    }).Append(new LineBreak())
                ).Append(new LineBreak())
            ).SkipLast(2))
        {
            StoryContnent.Inlines.Add(inline);
        }
    }
}
