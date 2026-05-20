using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Tests;

public sealed class TelegramSessionTests
{
    [Fact]
    public void MarkConnected_PersistsConnectedState()
    {
        DateTimeOffset connectedAt = DateTimeOffset.UtcNow;
        TelegramSession session = new() { IsEnabled = false, PendingChallenge = "password", LastError = "old error", };

        session.MarkConnected("encrypted-session", connectedAt);

        Assert.True(session.IsEnabled);
        Assert.Equal(TelegramSessionStatus.Connected, session.Status);
        Assert.Equal("encrypted-session", session.SessionDataEncrypted);
        Assert.Equal(connectedAt, session.LastConnectedAt);
        Assert.Null(session.PendingChallenge);
        Assert.Null(session.LastError);
    }

    [Fact]
    public void ResetForFreshLogin_ClearsTransientState()
    {
        TelegramSession session = new()
        {
            Status = TelegramSessionStatus.AwaitingPassword,
            PendingChallenge = "password",
            LastError = "bad state",
            SessionDataEncrypted = "old",
        };

        session.ResetForFreshLogin("empty");

        Assert.Equal(TelegramSessionStatus.Pending, session.Status);
        Assert.Equal("empty", session.SessionDataEncrypted);
        Assert.Null(session.PendingChallenge);
        Assert.Null(session.LastError);
    }

    [Fact]
    public void ApplyLoginProgress_WhenConnectedWithoutTimestamp_SetsLastConnectedAt()
    {
        TelegramSession session = new() { LastError = "old error" };

        session.ApplyLoginProgress(null, TelegramSessionStatus.Connected, connectedAt: null);

        Assert.Equal(TelegramSessionStatus.Connected, session.Status);
        Assert.NotNull(session.LastConnectedAt);
        Assert.Null(session.PendingChallenge);
        Assert.Null(session.LastError);
    }

    [Fact]
    public void MarkFaulted_StoresErrorAndFaultedStatus()
    {
        TelegramSession session = new();

        session.MarkFaulted("telegram rejected login");

        Assert.Equal(TelegramSessionStatus.Faulted, session.Status);
        Assert.Equal("telegram rejected login", session.LastError);
    }

    [Fact]
    public void ApplyLoginProgress_WhenNotConnected_DoesNotReplaceLastConnectedAt()
    {
        DateTimeOffset connectedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        TelegramSession session = new() { LastConnectedAt = connectedAt };

        session.ApplyLoginProgress("code", TelegramSessionStatus.AwaitingCode, DateTimeOffset.UtcNow);

        Assert.Equal(TelegramSessionStatus.AwaitingCode, session.Status);
        Assert.Equal("code", session.PendingChallenge);
        Assert.Equal(connectedAt, session.LastConnectedAt);
    }

    [Fact]
    public void ApplyLoginProgress_WhenConnectedWithTimestamp_UsesProvidedTimestamp()
    {
        DateTimeOffset connectedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        TelegramSession session = new();

        session.ApplyLoginProgress(null, TelegramSessionStatus.Connected, connectedAt);

        Assert.Equal(connectedAt, session.LastConnectedAt);
    }

    [Fact]
    public void DisableEnableAndClearError_UpdateExpectedFlags()
    {
        TelegramSession session = new() { LastError = "boom" };

        session.Disable();
        Assert.False(session.IsEnabled);
        Assert.Equal(TelegramSessionStatus.Disabled, session.Status);

        session.Enable();
        session.ClearError();

        Assert.True(session.IsEnabled);
        Assert.Null(session.LastError);
        Assert.Equal(TelegramSessionStatus.Disabled, session.Status);
    }
}
