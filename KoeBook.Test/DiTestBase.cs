using KoeBook.Core;
using KoeBook.Core.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KoeBook.Test;

public class DiTestBase
{
    protected IHost Host { get; init; } = CreateDefaultBuilder().Build();

    protected T GetService<T>() where T : notnull
    {
        return Host.Services.GetRequiredService<T>();
    }

    protected static IHostBuilder CreateDefaultBuilder()
    {
        return Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .UseCoreStartup()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ICreateCoverFileService, MockCreateCoverFileService>();
            });
    }
}
