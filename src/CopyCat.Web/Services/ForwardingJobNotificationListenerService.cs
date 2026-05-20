using System.Diagnostics.CodeAnalysis;

using Npgsql;

namespace CopyCat.Web.Services;

internal sealed class ForwardingJobNotificationListenerService(
    IConfiguration configuration,
    ForwardingJobUpdateNotifier notifier,
    ILogger<ForwardingJobNotificationListenerService> logger) : BackgroundService
{
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The listener must reconnect after transient database failures.")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string connectionString = configuration.GetConnectionString("CopyCat")
                                  ?? throw new InvalidOperationException(
                                      "Connection string 'CopyCat' is not configured.");

        while (!stoppingToken.IsCancellationRequested)
        {
            await using NpgsqlConnection connection = new(connectionString);
            try
            {
                connection.Notification += OnNotification;
                await connection.OpenAsync(stoppingToken);

                await using NpgsqlCommand listenCommand = new("LISTEN copycat_forwarding_jobs_changed;", connection);
                _ = await listenCommand.ExecuteNonQueryAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Forwarding job notification listener failed");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                connection.Notification -= OnNotification;
            }
        }

        void OnNotification(object sender, NpgsqlNotificationEventArgs eventArgs)
        {
            _ = notifier.NotifyJobsChangedAsync();
        }
    }
}
