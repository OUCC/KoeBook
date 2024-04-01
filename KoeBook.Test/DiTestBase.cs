using KoeBook.Core;
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
            .UseCoreStartup();
    }
}
