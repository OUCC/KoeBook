using KoeBook.ViewModels;
using Microsoft.UI.Xaml.Controls;

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

        this.InitializeComponent();
    }
}
