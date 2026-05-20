using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a queued Telegram control operation.
/// </summary>
public sealed class TelegramControlOperation : IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the operation type.
    /// </summary>
    public TelegramControlOperationType OperationType { get; set; }

    /// <summary>
    /// Gets or sets the operation status.
    /// </summary>
    public TelegramControlOperationStatus Status { get; set; } = TelegramControlOperationStatus.Pending;

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Gets or sets the source channel identifier.
    /// </summary>
    public Guid? SourceChannelId { get; set; }

    /// <summary>
    /// Gets or sets the mapping identifier.
    /// </summary>
    public Guid? MappingId { get; set; }

    /// <summary>
    /// Gets or sets the payload JSON.
    /// </summary>
    public string? PayloadJson { get; set; }

    /// <summary>
    /// Gets or sets the result JSON.
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>
    /// Gets or sets the last error.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the number of attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the completion time.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the maximum number of attempts before the operation is failed permanently.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the earliest time the operation may be retried (null = immediately eligible).
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    // -- Behaviour ------------------------------------------------------------

    /// <summary>
    /// Marks the operation as in-progress, recording the start time and incrementing the attempt counter.
    /// </summary>
    public void Begin()
    {
        Status = TelegramControlOperationStatus.Processing;
        StartedAt = DateTimeOffset.UtcNow;
        Attempts += 1;
    }

    /// <summary>
    /// Marks the operation as successfully completed.
    /// </summary>
    /// <param name="resultJson">Optional JSON result payload to persist alongside the operation.</param>
    public void Complete(string? resultJson)
    {
        Status = TelegramControlOperationStatus.Succeeded;
        ResultJson = resultJson;
        LastError = null;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Records a transient failure and either reschedules the operation for retry or marks it as
    /// permanently failed when the attempt limit is reached.
    /// </summary>
    /// <param name="error">The error message from the failed attempt.</param>
    /// <param name="retryDelay">The delay to wait before the next attempt.</param>
    public void RecordRetry(string error, TimeSpan retryDelay)
    {
        LastError = error;

        if (Attempts < MaxAttempts)
        {
            Status = TelegramControlOperationStatus.Pending;
            NextRetryAt = DateTimeOffset.UtcNow.Add(retryDelay);
        }
        else
        {
            Status = TelegramControlOperationStatus.Failed;
            CompletedAt = DateTimeOffset.UtcNow;
        }
    }
}
