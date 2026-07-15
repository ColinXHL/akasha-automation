using AkashaAutomation.Worker.Configuration;
using AkashaAutomation.Worker.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AkashaAutomation.Worker.Hosting;

public static class WorkerHost
{
    public static async Task<int> RunAsync(
        WorkerLaunchOptions options,
        IParentProcessLifetime parentProcess,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(parentProcess);

        var builder = Host.CreateApplicationBuilder(
            new HostApplicationBuilderSettings
            {
                ApplicationName = typeof(WorkerHost).Assembly.GetName().Name,
                Args = [],
                DisableDefaults = true,
            });

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddProvider(
            new JsonRollingFileLoggerProvider(
                new JsonRollingFileLoggerOptions(WorkerLogPaths.GetDefaultLogFilePath())));

        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(parentProcess);
        builder.Services.AddHostedService<WorkerExceptionObserver>();
        builder.Services.AddSingleton<WorkerRuntime>(services =>
            new WorkerRuntime(
                services.GetServices<IWorkerRuntimeResource>(),
                services.GetRequiredService<ILoggerFactory>()));
        builder.Services.AddSingleton<WorkerApplication>(services =>
            new WorkerApplication(
                services.GetRequiredService<IParentProcessLifetime>(),
                services.GetRequiredService<WorkerRuntime>(),
                services.GetRequiredService<ILogger<WorkerApplication>>()));

        using var host = builder.Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await host.Services
                .GetRequiredService<WorkerApplication>()
                .RunAsync(options, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
