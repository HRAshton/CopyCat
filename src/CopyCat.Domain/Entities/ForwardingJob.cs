using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a queued forwarding job.
/// </summary>
public sealed class ForwardingJob : IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the mapping identifier.
    /// </summary>
    public Guid MappingId { get; set; }

    /// <summary>
    /// Gets or sets the filter version identifier.
    /// </summary>
    public Guid? FilterVersionId { get; set; }

    /// <summary>
    /// Gets or sets the rewrite version identifier.
    /// </summary>
    public Guid? RewriteVersionId { get; set; }

    /// <summary>
    /// Gets or sets the job status.
    /// </summary>
    public ForwardingJobStatus Status { get; set; } = ForwardingJobStatus.Pending;

    /// <summary>
    /// Gets or sets the forwarding mode.
    /// </summary>
    public ForwardingMode ForwardingMode { get; set; }

    /// <summary>
    /// Gets or sets the number of attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the last error.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the next retry time.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    /// <summary>
    /// Gets or sets the target Telegram message identifier.
    /// </summary>
    public long? TargetTelegramMessageId { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // -- Behaviour ------------------------------------------------------------

    /// <summary>
    /// Marks the job as currently being processed.
    /// </summary>
    public void MarkProcessing()
    {
        Status = ForwardingJobStatus.Processing;
    }

    /// <summary>
    /// Marks the job as successfully forwarded.
    /// </summary>
    /// <param name="targetMessageId">The Telegram message-id of the forwarded copy.</param>
    public void MarkSucceeded(long? targetMessageId)
    {
        Status = ForwardingJobStatus.Succeeded;
        TargetTelegramMessageId = targetMessageId;
        LastError = null;
        NextRetryAt = null;
    }

    /// <summary>
    /// Records a processing failure, advancing the attempt counter and rescheduling
    /// the job for a later retry (or marking it permanently failed when the limit is reached).
    /// </summary>
    /// <param name="error">The error message from the failed attempt.</param>
    /// <param name="maxAttempts">Maximum total attempts before permanent failure.</param>
    public void RecordAttemptFailure(string error, int maxAttempts)
    {
        Attempts += 1;
        LastError = error;

        if (Attempts >= maxAttempts)
        {
            Status = ForwardingJobStatus.FailedPermanent;
            NextRetryAt = null;
        }
        else
        {
            Status = ForwardingJobStatus.FailedTransient;

            // Exponential back-off: 2^attempts minutes, capped at 30 minutes.
            NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(Math.Min(30, Math.Pow(2, Attempts)));
        }
    }
}
