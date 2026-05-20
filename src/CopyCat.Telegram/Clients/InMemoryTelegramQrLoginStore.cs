using System.Collections.Concurrent;

using CopyCat.Application.Abstractions;

namespace CopyCat.Telegram.Clients;

internal sealed class InMemoryTelegramQrLoginStore : ITelegramQrLoginStore
{
    private readonly ConcurrentDictionary<Guid, string> urls = new();

    public void Clear(Guid sessionId)
    {
        urls.TryRemove(sessionId, out _);
    }

    public string? GetUrl(Guid sessionId)
    {
        return urls.TryGetValue(sessionId, out string? url) ? url : null;
    }

    public void SetUrl(Guid sessionId, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        urls[sessionId] = url;
    }
}
