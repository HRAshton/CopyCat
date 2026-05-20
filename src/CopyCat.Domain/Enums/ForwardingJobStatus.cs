namespace CopyCat.Domain.Enums;

/// <summary>
/// Represents the processing lifecycle of a forwarding job.
/// </summary>
public enum ForwardingJobStatus
{
    /// <summary>Job is queued and waiting to be picked up.</summary>
    Pending,

    /// <summary>Job is currently being executed.</summary>
    Processing,

    /// <summary>Job completed successfully.</summary>
    Succeeded,

    /// <summary>Job failed but will be retried.</summary>
    FailedTransient,

    /// <summary>Job failed permanently and will not be retried.</summary>
    FailedPermanent,

    /// <summary>Job was cancelled before execution.</summary>
    Cancelled,

    /// <summary>Message was not forwarded (e.g. filtered out after job creation).</summary>
    Skipped,
}
