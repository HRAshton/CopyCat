using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;

namespace CopyCat.Worker.Services;

internal sealed class LiveMessageIngestWorker(
    IServiceProvider serviceProvider,
    ILogger<LiveMessageIngestWorker> logger) : BackgroundService
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Background workers must catch and log unexpected failures to keep the process alive.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                ILiveIngestProcessingService liveIngestProcessingService =
                    scope.ServiceProvider.GetRequiredService<ILiveIngestProcessingService>();
                await liveIngestProcessingService.ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Live ingest worker failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
