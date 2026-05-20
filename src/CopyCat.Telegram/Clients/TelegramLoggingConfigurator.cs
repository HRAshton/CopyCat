using CopyCat.Application.Abstractions;

using Microsoft.Extensions.Logging;

using WTelegram;

namespace CopyCat.Telegram.Clients;

internal static class TelegramLoggingConfigurator
{
    private static readonly AsyncLocal<SessionTraceContext?> CurrentTrace = new();

    private static int configured;

    internal static void EnsureConfigured(ILogger logger)
    {
        if (Interlocked.Exchange(ref configured, 1) == 1)
        {
            return;
        }

        Helpers.Log = (_, message) =>
        {
            SessionTraceContext? traceContext = CurrentTrace.Value;
            traceContext?.TraceStore.Record(traceContext.SessionId, message);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Message}", message);
            }
        };
    }

    internal static IDisposable BeginSessionTrace(Guid sessionId, ITelegramAuthTraceStore traceStore)
    {
        SessionTraceContext? previous = CurrentTrace.Value;
        CurrentTrace.Value = new SessionTraceContext(sessionId, traceStore);
        return new RestoreTraceScope(previous);
    }

    private sealed record SessionTraceContext(Guid SessionId, ITelegramAuthTraceStore TraceStore);

    private sealed class RestoreTraceScope(SessionTraceContext? previous) : IDisposable
    {
        public void Dispose()
        {
            CurrentTrace.Value = previous;
        }
    }
}
