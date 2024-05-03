using KoeBook.Core.Contracts.Services;
using KoeBook.Core.Services;
using KoeBook.Epub.Contracts.Services;
using KoeBook.Epub.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KoeBook.Core;

internal static class Startup
{
    /// <summary>
    /// System, Core, Epub のDIを登録します
    /// </summary>
    public static IHostBuilder UseCoreStartup(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            // System
            services.AddSingleton(TimeProvider.System);

            // Core Services
            services.AddHttpClient()
                .ConfigureHttpClientDefaults(builder =>
                {
                    builder.SetHandlerLifetime(TimeSpan.FromMinutes(5));
                });
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ISecretSettingsService, SecretSettingsService>();
            services.AddSingleton<IStyleBertVitsClientService, StyleBertVitsClientService>();
            services.AddSingleton<ISoundGenerationSelectorService, SoundGenerationSelectorService>();
            services.AddSingleton<ISoundGenerationService, SoundGenerationService>();
            services.AddSingleton<IEpubGenerateService, EpubGenerateService>();
            services.AddSingleton<IEpubDocumentStoreService, EpubDocumentStoreService>();
            services.AddSingleton<IAnalyzerService, AnalyzerService>();
            services.AddSingleton<ILlmAnalyzerService, ClaudeAnalyzerService>();
            services.AddSingleton<IClaudeService, ClaudeService>();

            // Epub Services
            services
                .AddKeyedSingleton<IScrapingClientService, ScrapingClientService>(nameof(ScrapingAozoraService))
                .AddKeyedSingleton<IScrapingClientService, ScrapingClientService>(nameof(ScrapingNaroService))
                .AddSingleton<IScraperSelectorService, ScraperSelectorService>()
                .AddSingleton<IScrapingService, ScrapingAozoraService>()
                .AddSingleton<IScrapingService, ScrapingNaroService>()
                .AddSingleton<AiStoryAnalyzerService>()
                .AddSingleton<IS3UploadService, S3UploadService>()
                .AddSingleton<IStoryCreatorService, ClaudeStoryGeneratorService>();
            services.AddSingleton<IEpubCreateService, EpubCreateService>();
            services.AddSingleton<ISplitBraceService, SplitBraceService>();
            services.AddSingleton<IFileExtensionService, FileExtensionService>();
        });

        return builder;
    }
}
