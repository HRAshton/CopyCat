using CopyCat.Application.Abstractions;

using Microsoft.Extensions.DependencyInjection;

namespace CopyCat.Application.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCopyCatApplication_ReturnsOriginalCollection()
    {
        ServiceCollection services = new();

        IServiceCollection returned = CopyCat.Application.DependencyInjection.ServiceCollectionExtensions
            .AddCopyCatApplication(services);

        Assert.Same(services, returned);
    }

    [Fact]
    public void AddCopyCatApplication_RegistersUseCaseServices()
    {
        ServiceCollection services = new();

        CopyCat.Application.DependencyInjection.ServiceCollectionExtensions.AddCopyCatApplication(services);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ISessionManagementService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IChannelManagementService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IMappingManagementService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IFilterManagementService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRewriteManagementService));
    }
}
