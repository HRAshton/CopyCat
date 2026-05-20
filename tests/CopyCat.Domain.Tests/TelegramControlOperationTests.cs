using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Tests;

public sealed class TelegramControlOperationTests
{
    [Fact]
    public void Begin_SetsProcessingStateAndIncrementsAttempts()
    {
        TelegramControlOperation operation = new() { Attempts = 1 };

        operation.Begin();

        Assert.Equal(TelegramControlOperationStatus.Processing, operation.Status);
        Assert.Equal(2, operation.Attempts);
        Assert.NotNull(operation.StartedAt);
    }

    [Fact]
    public void Complete_SetsSucceededState_AndClearsError()
    {
        TelegramControlOperation operation = new() { LastError = "old error" };

        operation.Complete("{\"ok\":true}");

        Assert.Equal(TelegramControlOperationStatus.Succeeded, operation.Status);
        Assert.Equal("{\"ok\":true}", operation.ResultJson);
        Assert.Null(operation.LastError);
        Assert.NotNull(operation.CompletedAt);
    }

    [Fact]
    public void RecordRetry_WhenAttemptsRemain_RequeuesOperation()
    {
        TelegramControlOperation operation = new() { Attempts = 1, MaxAttempts = 3 };

        operation.RecordRetry("temporary", TimeSpan.FromSeconds(10));

        Assert.Equal(TelegramControlOperationStatus.Pending, operation.Status);
        Assert.Equal("temporary", operation.LastError);
        Assert.NotNull(operation.NextRetryAt);
        Assert.Null(operation.CompletedAt);
    }

    [Fact]
    public void RecordRetry_WhenAttemptsExhausted_FailsOperation()
    {
        TelegramControlOperation operation = new() { Attempts = 3, MaxAttempts = 3 };

        operation.RecordRetry("fatal", TimeSpan.FromSeconds(10));

        Assert.Equal(TelegramControlOperationStatus.Failed, operation.Status);
        Assert.Equal("fatal", operation.LastError);
        Assert.NotNull(operation.CompletedAt);
    }

    [Fact]
    public void OperationMetadata_CanRepresentQueuedBackfillRequest()
    {
        TelegramControlOperation operation = new()
        {
            OperationType = TelegramControlOperationType.RunBackfill,
            SessionId = Guid.NewGuid(),
            SourceChannelId = Guid.NewGuid(),
            MappingId = Guid.NewGuid(),
            PayloadJson = "{\"take\":250}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAt = DateTimeOffset.UtcNow,
            NextRetryAt = DateTimeOffset.UtcNow.AddMinutes(1),
        };

        Assert.Equal(TelegramControlOperationType.RunBackfill, operation.OperationType);
        Assert.NotNull(operation.SessionId);
        Assert.NotNull(operation.SourceChannelId);
        Assert.NotNull(operation.MappingId);
        Assert.Equal("{\"take\":250}", operation.PayloadJson);
        Assert.Equal(3, operation.MaxAttempts);
        Assert.NotNull(operation.NextRetryAt);
    }
}
