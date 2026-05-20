using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Stores;

internal sealed class EntityFrameworkTelegramControlOperationScheduler(CopyCatDbContext dbContext)
    : ITelegramControlOperationScheduler
{
    public async Task<bool> HasPendingAsync(
        Guid sessionId,
        TelegramControlOperationType operationType,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TelegramControlOperations.AnyAsync(
            x => x.SessionId == sessionId
                 && x.OperationType == operationType
                 && (x.Status == TelegramControlOperationStatus.Pending
                     || x.Status == TelegramControlOperationStatus.Processing),
            cancellationToken);
    }

    public async Task EnqueueAsync(
        TelegramControlOperation operation,
        CancellationToken cancellationToken = default)
    {
        await dbContext.TelegramControlOperations.AddAsync(operation, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
