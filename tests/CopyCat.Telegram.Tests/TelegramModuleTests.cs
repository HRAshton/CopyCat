using System.Reflection;

using CopyCat.Application.Abstractions;
using CopyCat.Telegram.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Telegram.Tests;

public sealed class TelegramModuleTests
{
    [Fact]
    public void AddCopyCatTelegram_RegistersExpectedServices()
    {
        ServiceCollection services = new();

        IServiceCollection returned = ServiceCollectionExtensions.AddCopyCatTelegram(services);

        Assert.Same(services, returned);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ITelegramAuthTraceStore)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ITelegramQrLoginStore)
                          && descriptor.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(ITelegramGateway)
                          && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AuthTraceStore_KeepsRecentEntries_AndIgnoresWhitespace()
    {
        ITelegramAuthTraceStore store = CreateInternal<ITelegramAuthTraceStore>(
            "CopyCat.Telegram.Clients.InMemoryTelegramAuthTraceStore");
        Guid sessionId = Guid.NewGuid();

        store.Record(sessionId, "   ");
        for (int index = 1; index <= 25; index++)
        {
            store.Record(sessionId, $"event {index}");
        }

        string? trace = store.GetLatest(sessionId);
        Assert.NotNull(trace);
        Assert.DoesNotContain("   ", trace, StringComparison.Ordinal);

        string[] lines = trace.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(20, lines.Length);
        Assert.DoesNotContain(lines, line => line.EndsWith("event 1", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.EndsWith("event 5", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.EndsWith("event 6", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.EndsWith("event 25", StringComparison.Ordinal));

        store.Clear(sessionId);

        Assert.Null(store.GetLatest(sessionId));
    }

    [Fact]
    public void QrLoginStore_StoresValidUrl_AndIgnoresBlankValues()
    {
        ITelegramQrLoginStore store = CreateInternal<ITelegramQrLoginStore>(
            "CopyCat.Telegram.Clients.InMemoryTelegramQrLoginStore");
        Guid sessionId = Guid.NewGuid();

        store.SetUrl(sessionId, "https://example.test/qr");
        store.SetUrl(sessionId, " ");

        Assert.Equal("https://example.test/qr", store.GetUrl(sessionId));

        store.Clear(sessionId);

        Assert.Null(store.GetUrl(sessionId));
    }

    private static TService CreateInternal<TService>(string typeName)
        where TService : class
    {
        Assembly assembly = typeof(ServiceCollectionExtensions).Assembly;
        Type type = assembly.GetType(typeName, throwOnError: true)!;
        object instance = Activator.CreateInstance(type, nonPublic: true)!;
        return Assert.IsAssignableFrom<TService>(instance);
    }
}
