namespace CopyCat.Application.Options;

/// <summary>
/// Configures timings and batch sizes for the background worker.
/// </summary>
public sealed class ApplicationWorkerOptions
{
    /// <summary>
    /// The configuration section key.
    /// </summary>
    public const string SectionKey = "Worker";

    /// <summary>
    /// Gets or sets the number of filtering jobs processed per worker tick.
    /// Defaults to 50.
    /// </summary>
    public int FilteringBatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the number of forwarding jobs processed per worker tick.
    /// Defaults to 20.
    /// </summary>
    public int ForwardingBatchSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the number of messages fetched per live-ingest tick per mapping.
    /// Defaults to 100.
    /// </summary>
    public int LiveIngestBatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the back-off delay applied to a control operation on transient failure,
    /// before it becomes eligible for retry.
    /// Defaults to 60 seconds.
    /// </summary>
    public TimeSpan ControlOperationRetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the age threshold for a <c>Processing</c> control operation before it
    /// is considered stuck and reset to <c>Pending</c>.
    /// Defaults to 5 minutes.
    /// </summary>
    public TimeSpan ControlOperationStuckThreshold { get; set; } = TimeSpan.FromMinutes(5);
}
