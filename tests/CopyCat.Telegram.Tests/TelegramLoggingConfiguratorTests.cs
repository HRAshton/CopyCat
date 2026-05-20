using CopyCat.Application.Abstractions;
using CopyCat.Telegram.Clients;

using Microsoft.Extensions.Logging.Abstractions;

using WTelegram;

namespace CopyCat.Telegram.Tests;

public sealed class TelegramLoggingConfiguratorTests
{
    [Fact]
    public void EnsureConfigured_SetsWTelegramLogDelegate()
    {
        // First call sets the delegate; second call is a no-op.
        // Both must be exercised to hit both branches of the Interlocked check.
        TelegramLoggingConfigurator.EnsureConfigured(NullLogger.Instance);
        TelegramLoggingConfigurator.EnsureConfigured(NullLogger.Instance);

        Assert.NotNull(Helpers.Log);
    }

    [Fact]
    public void BeginSessionTrace_CapturesToTraceStore_AndRestoresPreviousContextOnDispose()
    {
        RecordingTraceStore store = new();
        Guid sessionId = Guid.NewGuid();

        // Ensure the logger delegate is configured so invocations do not throw.
        TelegramLoggingConfigurator.EnsureConfigured(NullLogger.Instance);

        using (TelegramLoggingConfigurator.BeginSessionTrace(sessionId, store))
        {
            // Simulate a WTelegram log entry while the trace scope is active.
            // WTelegram passes an int log level as the first argument.
            Helpers.Log!(0, "trace-message");
        }

        Assert.Contains("trace-message", store.Messages);
    }

    [Fact]
    public void BeginSessionTrace_RestoresPreviousContext_WhenNested()
    {
        RecordingTraceStore outerStore = new();
        RecordingTraceStore innerStore = new();
        Guid outerSessionId = Guid.NewGuid();
        Guid innerSessionId = Guid.NewGuid();

        TelegramLoggingConfigurator.EnsureConfigured(NullLogger.Instance);

        using (TelegramLoggingConfigurator.BeginSessionTrace(outerSessionId, outerStore))
        {
            using (TelegramLoggingConfigurator.BeginSessionTrace(innerSessionId, innerStore))
            {
                Helpers.Log!(0, "inner-message");
            }

            // After the inner scope is disposed the outer context should be restored.
            Helpers.Log!(0, "outer-message");
        }

        Assert.Contains("inner-message", innerStore.Messages);
        Assert.Contains("outer-message", outerStore.Messages);
        Assert.DoesNotContain("outer-message", innerStore.Messages);
    }

    private sealed class RecordingTraceStore : ITelegramAuthTraceStore
    {
        public List<string> Messages { get; } = [];

        public string? GetLatest(Guid sessionId)
        {
            return string.Join("\n", Messages);
        }

        public void Record(Guid sessionId, string message)
        {
            Messages.Add(message);
        }

        public void Clear(Guid sessionId)
        {
            Messages.Clear();
        }
    }
}
