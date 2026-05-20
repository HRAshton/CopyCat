using System.Reflection;

using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Data.Migrations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace CopyCat.Infrastructure.Tests;

public sealed class MigrationCoverageTests
{
    [Fact]
    public void InitialCreate_Up_CreatesExpectedTablesAndIndexes()
    {
        InitialCreate migration = new();
        MigrationBuilder migrationBuilder = new("Npgsql.EntityFrameworkCore.PostgreSQL");

        InvokeProtected(migration, "Up", migrationBuilder);

        CreateTableOperation[] tables = migrationBuilder.Operations.OfType<CreateTableOperation>().ToArray();
        CreateIndexOperation[] indexes = migrationBuilder.Operations.OfType<CreateIndexOperation>().ToArray();

        Assert.Contains(tables, x => x.Name == "TelegramSessions");
        Assert.Contains(tables, x => x.Name == "TelegramChannels");
        Assert.Contains(tables, x => x.Name == "ChannelMappings");
        Assert.Contains(tables, x => x.Name == "FilterSets");
        Assert.Contains(tables, x => x.Name == "RewriteSets");
        Assert.Contains(indexes, x => x.Name == "IX_TelegramChannels_SessionId_TelegramChannelId" && x.IsUnique);
        Assert.Contains(indexes, x => x.Name == "IX_ChannelMappings_SourceChannelId_TargetChannelId" && x.IsUnique);
        Assert.Contains(indexes, x => x.Name == "IX_ForwardingJobs_Status_NextRetryAt");
    }

    [Fact]
    public void InitialCreate_Down_DropsExpectedTables()
    {
        InitialCreate migration = new();
        MigrationBuilder migrationBuilder = new("Npgsql.EntityFrameworkCore.PostgreSQL");

        InvokeProtected(migration, "Down", migrationBuilder);

        DropTableOperation[] tables = migrationBuilder.Operations.OfType<DropTableOperation>().ToArray();

        Assert.Contains(tables, x => x.Name == "TelegramSessions");
        Assert.Contains(tables, x => x.Name == "TelegramChannels");
        Assert.Contains(tables, x => x.Name == "ChannelMappings");
        Assert.Contains(tables, x => x.Name == "FilterSets");
        Assert.Contains(tables, x => x.Name == "RewriteSets");
    }

    [Fact]
    public void InitialCreate_BuildTargetModel_ContainsCurrentAggregateRoots()
    {
        InitialCreate migration = new();
        ModelBuilder modelBuilder = CreateModelBuilder();

        InvokeProtected(migration, "BuildTargetModel", modelBuilder);

        IMutableModel model = modelBuilder.Model;
        Assert.NotNull(model.FindEntityType("CopyCat.Domain.Entities.TelegramSession"));
        Assert.NotNull(model.FindEntityType("CopyCat.Domain.Entities.TelegramChannel"));
        Assert.NotNull(model.FindEntityType("CopyCat.Domain.Entities.ChannelMapping"));
        Assert.NotNull(model.FindEntityType("CopyCat.Domain.Entities.FilterSet"));
        Assert.NotNull(model.FindEntityType("CopyCat.Domain.Entities.RewriteSet"));
    }

    [Fact]
    public void Snapshot_BuildModel_ReflectsCurrentSchemaShape()
    {
        CopyCatDbContextModelSnapshot snapshot = new();
        ModelBuilder modelBuilder = CreateModelBuilder();

        InvokeProtected(snapshot, "BuildModel", modelBuilder);

        IMutableModel model = modelBuilder.Model;
        IEntityType filterSet = Assert.IsAssignableFrom<IEntityType>(
            model.FindEntityType("CopyCat.Domain.Entities.FilterSet"));
        IEntityType channelMapping = Assert.IsAssignableFrom<IEntityType>(
            model.FindEntityType("CopyCat.Domain.Entities.ChannelMapping"));

        Assert.Null(filterSet.FindProperty("IsActive"));
        Assert.NotNull(channelMapping.FindProperty("ActiveFilterSetId"));
        Assert.NotNull(channelMapping.FindIndex(
            [
                channelMapping.FindProperty("SourceChannelId")!,
                channelMapping.FindProperty("TargetChannelId")!,
            ]));
    }

    private static void InvokeProtected(object instance, string methodName, object parameter)
    {
        MethodInfo method = instance.GetType().GetMethod(
                                methodName,
                                BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException($"Method '{methodName}' was not found.");
        method.Invoke(instance, [parameter]);
    }

    private static ModelBuilder CreateModelBuilder()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=copycat;Username=copycat;Password=copycat")
            .Options;
        using CopyCatDbContext dbContext = new(options);
        IConventionSetBuilder conventionSetBuilder = dbContext.GetService<IConventionSetBuilder>();
        return new ModelBuilder(conventionSetBuilder.CreateConventionSet());
    }
}
