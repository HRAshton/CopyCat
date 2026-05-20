using CopyCat.Application.Abstractions;
using CopyCat.Application.Abstractions.Ports;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace CopyCat.Infrastructure.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCopyCatInfrastructure_RegistersCoreServices()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new DictionaryConfiguration(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:CopyCat"] = "Host=localhost;Database=copycat;Username=copycat;Password=copycat",
                ["Security:ApplicationName"] = "CopyCat.Tests",
            });

        CopyCat.Infrastructure.DependencyInjection.ServiceCollectionExtensions
            .AddCopyCatInfrastructure(services, configuration);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFilterEvaluator));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMessageRewriter));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ISecretProtector));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ITelegramInteractiveLoginSink));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ISessionStore));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IChannelStore));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMappingStore));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFilterSetStore));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRewriteSetStore));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ITelegramControlOperationScheduler));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMappingRuleEditor));
    }

    private sealed class DictionaryConfiguration(IReadOnlyDictionary<string, string?> values) : IConfiguration
    {
        public string? this[string key]
        {
            get { return values.TryGetValue(key, out string? value) ? value : null; }

            set { throw new NotSupportedException(); }
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return [];
        }

        public IChangeToken GetReloadToken()
        {
            throw new NotSupportedException();
        }

        public IConfigurationSection GetSection(string key)
        {
            return new DictionaryConfigurationSection(key, this[key]);
        }
    }

    private sealed class DictionaryConfigurationSection(string key, string? value) : IConfigurationSection
    {
        public string Key => key;

        public string Path => key;

        public string? Value
        {
            get { return value; }

            set { throw new NotSupportedException(); }
        }

        public string? this[string key]
        {
            get { throw new NotSupportedException(); }

            set { throw new NotSupportedException(); }
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return [];
        }

        public IChangeToken GetReloadToken()
        {
            throw new NotSupportedException();
        }

        public IConfigurationSection GetSection(string sectionKey)
        {
            return new DictionaryConfigurationSection(sectionKey, null);
        }
    }
}
