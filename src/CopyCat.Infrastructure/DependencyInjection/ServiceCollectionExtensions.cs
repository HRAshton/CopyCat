using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Security;
using CopyCat.Infrastructure.Services;
using CopyCat.Infrastructure.Stores;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Infrastructure.DependencyInjection;

/// <summary>
/// Provides extension methods for registering CopyCat infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CopyCat infrastructure services with the dependency-injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddCopyCatInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CopyCatSecurityOptions>(options =>
        {
            options.ApplicationName = configuration[$"{CopyCatSecurityOptions.SectionName}:ApplicationName"]
                                      ?? "CopyCat";
        });
        services.AddDbContext<CopyCatDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("CopyCat"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
        });

        services.AddDataProtection()
            .PersistKeysToDbContext<CopyCatDbContext>()
            .SetApplicationName(configuration[$"{CopyCatSecurityOptions.SectionName}:ApplicationName"] ?? "CopyCat");

        services.AddScoped<ISecretProtector>(serviceProvider =>
        {
            IDataProtectionProvider provider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
            return new DataProtectionSecretProtector(provider.CreateProtector("CopyCat.Secrets"));
        });

        services.AddScoped<IFilterEvaluator, FilterEvaluator>();
        services.AddScoped<IMessageRewriter, MessageRewriter>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddSingleton<ITelegramInteractiveLoginSink, TelegramInteractiveLoginSink>();
        services.AddScoped<ISessionStore, EntityFrameworkSessionStore>();
        services.AddScoped<IChannelStore, EntityFrameworkChannelStore>();
        services.AddScoped<IMappingStore, EntityFrameworkMappingStore>();
        services.AddScoped<IFilterSetStore, EntityFrameworkFilterSetStore>();
        services.AddScoped<IRewriteSetStore, EntityFrameworkRewriteSetStore>();
        services.AddScoped<IFilteringWorkStore, EntityFrameworkFilteringWorkStore>();
        services.AddScoped<IForwardingWorkStore, EntityFrameworkForwardingWorkStore>();
        services.AddScoped<ILiveIngestWorkStore, EntityFrameworkLiveIngestWorkStore>();
        services.AddScoped<ITelegramControlOperationWorkStore, EntityFrameworkTelegramControlOperationWorkStore>();
        services.AddScoped<ITelegramControlOperationScheduler, EntityFrameworkTelegramControlOperationScheduler>();
        services.AddScoped<IMappingRuleEditor, MappingRuleEditorService>();
        services.AddScoped<IMappingRuleEditorStore, EntityFrameworkMappingRuleEditorStore>();
        services.AddScoped<IMessageRoutingService, MessageRoutingService>();
        services.AddScoped<IMessageHistoryService, MessageHistoryService>();
        services.AddScoped<IForwardingJobService, ForwardingJobService>();
        return services;
    }
}
