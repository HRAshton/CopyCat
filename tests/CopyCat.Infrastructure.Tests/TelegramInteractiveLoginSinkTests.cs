using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Infrastructure.Tests;

public sealed class TelegramInteractiveLoginSinkTests
{
    [Fact]
    public async Task CompleteLoginAsync_UpdatesSessionStateAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new()
        {
            Name = "Session",
            Status = TelegramSessionStatus.Pending,
            PendingChallenge = "qr_login",
            LastError = "old",
        };
        Guid sessionId = session.Id;
        await dbContext.TelegramSessions.AddAsync(session);
        await dbContext.SaveChangesAsync();

        TelegramInteractiveLoginSink sink = new(new TestScopeFactory(dbContext));
        DateTimeOffset connectedAt = DateTimeOffset.UtcNow;

        await sink.CompleteLoginAsync(sessionId, "encrypted-session", connectedAt);
        await sink.CompleteLoginAsync(Guid.NewGuid(), "ignored", connectedAt);

        TelegramSession stored = await dbContext.TelegramSessions.SingleAsync(x => x.Id == sessionId);
        Assert.Equal(TelegramSessionStatus.Connected, stored.Status);
        Assert.True(stored.IsEnabled);
        Assert.Equal("encrypted-session", stored.SessionDataEncrypted);
        Assert.Null(stored.PendingChallenge);
        Assert.Null(stored.LastError);
        Assert.Equal(connectedAt, stored.LastConnectedAt);
    }

    [Fact]
    public async Task FailLoginAsync_SetsFaultedStatusAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramSession session = new() { Name = "Session", Status = TelegramSessionStatus.AwaitingCode, };
        Guid sessionId = session.Id;
        await dbContext.TelegramSessions.AddAsync(session);
        await dbContext.SaveChangesAsync();

        TelegramInteractiveLoginSink sink = new(new TestScopeFactory(dbContext));

        await sink.FailLoginAsync(sessionId, "boom");
        await sink.FailLoginAsync(Guid.NewGuid(), "ignored");

        TelegramSession stored = await dbContext.TelegramSessions.SingleAsync(x => x.Id == sessionId);
        Assert.Equal(TelegramSessionStatus.Faulted, stored.Status);
        Assert.Equal("boom", stored.LastError);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CopyCatDbContext(options);
    }

    private sealed class TestScopeFactory(CopyCatDbContext dbContext) : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            return new TestScope(dbContext);
        }
    }

    private sealed class TestScope(CopyCatDbContext dbContext) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new TestServiceProvider(dbContext);

        public void Dispose()
        {
        }
    }

    private sealed class TestServiceProvider(CopyCatDbContext dbContext) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(CopyCatDbContext) ? dbContext : null;
        }
    }
}
