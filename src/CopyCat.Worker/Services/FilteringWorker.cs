using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;

namespace CopyCat.Worker.Services;

internal sealed class FilteringWorker(
    IServiceProvider serviceProvider,
    ILogger<FilteringWorker> logger) : BackgroundService
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
                IMessageFilteringService filteringService =
                    scope.ServiceProvider.GetRequiredService<IMessageFilteringService>();
                await filteringService.ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Filtering worker failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
