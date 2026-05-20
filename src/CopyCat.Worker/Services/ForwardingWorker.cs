using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;

namespace CopyCat.Worker.Services;

internal sealed class ForwardingWorker(
    IServiceProvider serviceProvider,
    ILogger<ForwardingWorker> logger) : BackgroundService
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
                IForwardingProcessingService forwardingProcessingService =
                    scope.ServiceProvider.GetRequiredService<IForwardingProcessingService>();
                await forwardingProcessingService.ProcessBatchAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Forwarding worker loop failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }
}
