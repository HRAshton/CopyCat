using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Application.Abstractions.Ports;

/// <summary>
/// Queues asynchronous Telegram control operations for the worker.
/// </summary>
public interface ITelegramControlOperationScheduler
{
    /// <summary>
    /// Determines whether a matching operation is already pending or processing.
    /// </summary>
    /// <param name="sessionId">The session the operation belongs to.</param>
    /// <param name="operationType">The control operation type to look for.</param>
    /// <param name="cancellationToken">The cancellation token for the database operation.</param>
    /// <returns><see langword="true"/> when a matching operation is already queued or running; otherwise, <see langword="false"/>.</returns>
    Task<bool> HasPendingAsync(
        Guid sessionId,
        TelegramControlOperationType operationType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a control operation.
    /// </summary>
    /// <param name="operation">The control operation entity to enqueue.</param>
    /// <param name="cancellationToken">The cancellation token for the save operation.</param>
    /// <returns>A task that completes when the control operation has been queued.</returns>
    Task EnqueueAsync(TelegramControlOperation operation, CancellationToken cancellationToken = default);
}
