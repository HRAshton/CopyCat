using System.Collections.Concurrent;

using CopyCat.Application.Abstractions;

namespace CopyCat.Telegram.Clients;

internal sealed class InMemoryTelegramAuthTraceStore : ITelegramAuthTraceStore
{
    private const int MaxEntriesPerSession = 20;

    private readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> traces = new();

    public void Clear(Guid sessionId)
    {
        traces.TryRemove(sessionId, out _);
    }

    public void Record(Guid sessionId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        ConcurrentQueue<string> queue = traces.GetOrAdd(sessionId, _ => new ConcurrentQueue<string>());
        queue.Enqueue($"[{DateTimeOffset.UtcNow:HH:mm:ss}] {message.Trim()}");
        while (queue.Count > MaxEntriesPerSession && queue.TryDequeue(out _))
        {
        }
    }

    public string? GetLatest(Guid sessionId)
    {
        if (!traces.TryGetValue(sessionId, out ConcurrentQueue<string>? queue))
        {
            return null;
        }

        string[] entries = queue.ToArray();
        return entries.Length == 0 ? null : string.Join(Environment.NewLine, entries);
    }
}
