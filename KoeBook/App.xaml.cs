using FastEnumUtility;
using KoeBook.Activation;
using KoeBook.Components.Dialog;
using KoeBook.Contracts.Services;
using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Services;
using KoeBook.Core.Services.Mocks;
using KoeBook.Epub;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Services;
using KoeBook.Models;
using KoeBook.Notifications;
using KoeBook.Services;
using KoeBook.Services.CoreMocks;
using KoeBook.ViewModels;
using KoeBook.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace KoeBook;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

    public static UIElement? AppTitlebar { get; set; }

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .UseCoreStartup()
            .ConfigureServices((context, services) =>
            {
                // Default Activation Handler
                services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

                // Other Activation Handlers
                services.AddTransient<IActivationHandler, AppNotificationActivationHandler>();

                // Services
                services.AddSingleton<IAppNotificationService, AppNotificationService>();
                services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<IApiRootSelectorService, ApiRootSelectorService>();
                services.AddTransient<INavigationViewService, NavigationViewService>();
                services.AddSingleton<ITabViewService, TabViewService>();

                services.AddSingleton<IGenerationTaskService, GenerationTaskService>();
                services.AddSingleton<GenerationTaskRunnerService>();
                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IDisplayStateChangeService, DisplayStateChangeService>();

                // Views and ViewModels
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<TaskListViewModel>();
                services.AddTransient<GenerationTaskViewModel>();
                services.AddTransient<MainPage>();
                services.AddTransient<ShellPage>();
                services.AddTransient<ShellViewModel>();
                services.AddTransient<EditDetailsViewModel>();

                // Configuration
                services.Configure<LocalSettingsOptions>(context.Configuration.GetSection(nameof(LocalSettingsOptions)));

                // Core Services Mock
                var mockOptions = context.Configuration.GetSection(nameof(MockOptions)).Get<MockOptions>()!;
                if (mockOptions.IAnalyzerService.HasValue && mockOptions.IAnalyzerService.Value)
                    services.AddSingleton<IAnalyzerService, AnalyzerServiceMock>();
                if (mockOptions.IEpubGenerateService.HasValue && mockOptions.IEpubGenerateService.Value)
                    services.AddSingleton<IEpubGenerateService, EpubGenerateServiceMock>();
                if (mockOptions.ISoundGenerationSelectorService.HasValue && mockOptions.ISoundGenerationSelectorService.Value)
                    services.AddSingleton<ISoundGenerationSelectorService, SoundGenerationSelectorServiceMock>();
                if (mockOptions.ISoundGenerationService.HasValue && mockOptions.ISoundGenerationService.Value)
                    services.AddSingleton<ISoundGenerationService, SoundGenerationServiceMock>();
            })
            .Build();

        App.GetService<IAppNotificationService>().Initialize();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var hwnd = WindowNative.GetWindowHandle(MainWindow);
        if (e.Exception is EbookException ebookException)
        {
            PInvoke.MessageBox((HWND)hwnd,
                $"ラーが発生しました。KoeBookを終了します。\n{ebookException.ExceptionType.GetEnumMemberValue()}",
                "KoeBookからのお知らせ",
                MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING);
        }
        else
        {
            PInvoke.MessageBox((HWND)hwnd,
                $"不明なエラーが発生しました。KoeBookを終了します。\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "KoeBookからのお知らせ",
                MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONWARNING);
        }
        e.Handled = true;
        Current.Exit();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await GetService<IActivationService>().ActivateAsync(args);
    }
}
