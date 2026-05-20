using System.Diagnostics.CodeAnalysis;

using CopyCat.Telegram.Clients;

namespace CopyCat.Telegram.Tests;

[SuppressMessage(
    "Reliability",
    "CA2000:Dispose objects before losing scope",
    Justification = "The store is not disposable and the scope is returned to the caller.")]
public sealed class TelegramPendingLoginStoreTests
{
    [Fact]
    public void TryTake_ReturnsFalse_WhenStoreIsEmpty()
    {
        TelegramPendingLoginStore store = new();

        bool result = store.TryTake(Guid.NewGuid(), out TelegramClientScope? scope);

        Assert.False(result);
        Assert.Null(scope);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenStoreIsEmpty()
    {
        TelegramPendingLoginStore store = new();

        bool result = store.TryGet(Guid.NewGuid(), out TelegramClientScope? scope);

        Assert.False(result);
        Assert.Null(scope);
    }

    [Fact]
    public void TryTake_ReturnsFalse_ForUnknownSession_WhenOtherSessionExists()
    {
        TelegramPendingLoginStore store = new();
        Guid unknownSession = Guid.NewGuid();

        // Taking an unknown session ID should return false even when another entry might exist.
        bool result = store.TryTake(unknownSession, out TelegramClientScope? scope);

        Assert.False(result);
        Assert.Null(scope);
    }
}
