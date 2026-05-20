using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;

namespace CopyCat.Worker.Services;

internal sealed class TelegramControlOperationWorker(
    IServiceProvider serviceProvider,
    ILogger<TelegramControlOperationWorker> logger) : BackgroundService
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
                ITelegramControlOperationProcessingService operationProcessingService =
                    scope.ServiceProvider.GetRequiredService<ITelegramControlOperationProcessingService>();
                bool processed = await operationProcessingService.ProcessNextAsync(stoppingToken);
                if (!processed)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Telegram control operation worker failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
