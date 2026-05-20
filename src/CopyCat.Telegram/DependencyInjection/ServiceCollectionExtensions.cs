using CopyCat.Application.Abstractions;
using CopyCat.Telegram.Clients;

using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Telegram.DependencyInjection;

/// <summary>
/// Provides extension methods for registering CopyCat Telegram services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CopyCat Telegram client services with the dependency-injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCopyCatTelegram(this IServiceCollection services)
    {
        services.AddSingleton<ITelegramAuthTraceStore, InMemoryTelegramAuthTraceStore>();
        services.AddSingleton<ITelegramQrLoginStore, InMemoryTelegramQrLoginStore>();
        services.AddSingleton<TelegramPendingLoginStore>();
        services.AddScoped<ITelegramGateway, WTelegramGateway>();
        return services;
    }
}
