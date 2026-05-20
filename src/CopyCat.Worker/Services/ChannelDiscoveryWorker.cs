using System.Diagnostics.CodeAnalysis;

using CopyCat.Application.Abstractions;

namespace CopyCat.Worker.Services;

internal sealed class ChannelDiscoveryWorker(
    IServiceProvider serviceProvider,
    ILogger<ChannelDiscoveryWorker> logger) : BackgroundService
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
                IChannelManagementService channels =
                    scope.ServiceProvider.GetRequiredService<IChannelManagementService>();
                await channels.DiscoverChannelsAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Channel discovery worker failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
