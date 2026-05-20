using System.Collections.Concurrent;

namespace CopyCat.Telegram.Clients;

/// <summary>
/// Stores live Telegram login clients between interactive authentication steps.
/// </summary>
public sealed class TelegramPendingLoginStore
{
    private readonly ConcurrentDictionary<Guid, TelegramClientScope> scopes = new();

    internal bool TryTake(Guid sessionId, out TelegramClientScope? scope)
    {
        return scopes.TryRemove(sessionId, out scope);
    }

    internal async Task ReplaceAsync(Guid sessionId, TelegramClientScope scope)
    {
        if (scopes.TryRemove(sessionId, out TelegramClientScope? previous))
        {
            await previous.DisposeAsync();
        }

        scopes[sessionId] = scope;
    }

    internal bool TryGet(Guid sessionId, out TelegramClientScope? scope)
    {
        return scopes.TryGetValue(sessionId, out scope);
    }

    internal async Task ClearAsync(Guid sessionId)
    {
        if (scopes.TryRemove(sessionId, out TelegramClientScope? scope))
        {
            await scope.DisposeAsync();
        }
    }
}
