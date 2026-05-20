using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CopyCat.Infrastructure.Data;

/// <summary>
/// Creates the application database context for design-time EF tooling.
/// </summary>
public sealed class CopyCatDbContextFactory : IDesignTimeDbContextFactory<CopyCatDbContext>
{
    /// <summary>
    /// Creates a database context instance.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The configured database context.</returns>
    public CopyCatDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CopyCat")
                                  ?? "Host=localhost;Port=5432;Database=copycat;Username=copycat;Password=copycat";

        DbContextOptionsBuilder<CopyCatDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString);

        return new CopyCatDbContext(optionsBuilder.Options);
    }
}
