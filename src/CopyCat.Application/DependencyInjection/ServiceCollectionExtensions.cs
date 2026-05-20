using CopyCat.Application.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Application.DependencyInjection;

/// <summary>
/// Provides extension methods for registering CopyCat application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CopyCat application use-case services with the dependency-injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCopyCatApplication(this IServiceCollection services)
    {
        services.AddScoped<ISessionManagementService, Services.SessionService>();
        services.AddScoped<IChannelManagementService, Services.ChannelService>();
        services.AddScoped<IMappingManagementService, Services.MappingService>();
        services.AddScoped<IFilterManagementService, Services.ApplicationFilterManagementService>();
        services.AddScoped<IRewriteManagementService, Services.ApplicationRewriteManagementService>();
        services.AddScoped<IMessageFilteringService, Services.ApplicationMessageFilteringService>();
        services.AddScoped<IForwardingProcessingService, Services.ApplicationForwardingProcessingService>();
        services.AddScoped<ILiveIngestProcessingService, Services.ApplicationLiveIngestProcessingService>();
        services
            .AddScoped<ITelegramControlOperationProcessingService,
                Services.ApplicationTelegramControlOperationProcessingService>();
        return services;
    }
}
