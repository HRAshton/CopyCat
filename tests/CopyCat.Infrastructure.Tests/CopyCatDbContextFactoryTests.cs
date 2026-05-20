using CopyCat.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class CopyCatDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_UsesDefaultConnectionStringWhenEnvironmentIsMissing()
    {
        string? original = Environment.GetEnvironmentVariable("ConnectionStrings__CopyCat");
        Environment.SetEnvironmentVariable("ConnectionStrings__CopyCat", null);

        try
        {
            CopyCatDbContextFactory factory = new();

            using CopyCatDbContext dbContext = factory.CreateDbContext([]);

            string connectionString = dbContext.Database.GetConnectionString()!;
            Assert.Contains("Database=copycat", connectionString, StringComparison.Ordinal);
            Assert.Contains("Username=copycat", connectionString, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__CopyCat", original);
        }
    }

    [Fact]
    public void CreateDbContext_UsesEnvironmentConnectionStringWhenProvided()
    {
        string? original = Environment.GetEnvironmentVariable("ConnectionStrings__CopyCat");
        const string connectionString = "Host=test;Port=9999;Database=custom;Username=user;Password=pass";
        Environment.SetEnvironmentVariable("ConnectionStrings__CopyCat", connectionString);

        try
        {
            CopyCatDbContextFactory factory = new();

            using CopyCatDbContext dbContext = factory.CreateDbContext([]);

            Assert.Equal(connectionString, dbContext.Database.GetConnectionString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__CopyCat", original);
        }
    }
}
