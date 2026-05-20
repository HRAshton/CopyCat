using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;
using CopyCat.Infrastructure.Data;
using CopyCat.Infrastructure.Services;

using Microsoft.EntityFrameworkCore;

namespace CopyCat.Infrastructure.Tests;

public sealed class MessageRoutingServiceTests
{
    [Fact]
    public async Task RouteMessageAsync_UsesPublishedVersions_AndCreatesPendingJobAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramChannel sourceChannel = CreateChannel("Source");
        TelegramChannel targetChannel = CreateChannel("Target");
        FilterSet filterSet = new();
        FilterVersion publishedFilter = new()
        {
            FilterSet = filterSet,
            VersionNumber = 1,
            Status = FilterVersionStatus.Published,
            FilterDefinition = FilterSetDefinition.AllowAll(),
        };
        FilterVersion draftFilter = new()
        {
            FilterSet = filterSet,
            VersionNumber = 2,
            Status = FilterVersionStatus.Draft,
            FilterDefinition = new FilterSetDefinition(
                "Reject text",
                MappingDefaultPolicy.Reject,
                new HasTextCondition(false, "reject")),
        };
        RewriteSet rewriteSet = new();
        RewriteVersion publishedRewrite = new()
        {
            RewriteSet = rewriteSet,
            VersionNumber = 1,
            Status = RewriteVersionStatus.Published,
            Rules = new RewriteRuleSet([new AppendFooterOperation("Footer")]),
        };
        RewriteVersion draftRewrite = new()
        {
            RewriteSet = rewriteSet,
            VersionNumber = 2,
            Status = RewriteVersionStatus.Draft,
            Rules = new RewriteRuleSet([new StripAllTextOperation()]),
        };
        ChannelMapping mapping = new()
        {
            SourceChannel = sourceChannel,
            TargetChannel = targetChannel,
            ActiveFilterSet = filterSet,
            ActiveRewriteSet = rewriteSet,
            ForwardingMode = ForwardingMode.CopyWithRewriting,
        };
        StoredMessage message = CreateMessage(sourceChannel.Id, "Hello world");

        await dbContext.AddRangeAsync(
            sourceChannel,
            targetChannel,
            filterSet,
            publishedFilter,
            draftFilter,
            rewriteSet,
            publishedRewrite,
            draftRewrite,
            mapping,
            message);
        await dbContext.SaveChangesAsync();

        MessageRoutingService service = new(dbContext, new FilterEvaluator(), new MessageRewriter());

        await service.RouteMessageAsync(message, mapping);

        MessageDecision decision = Assert.Single(dbContext.MessageDecisions);
        ForwardingJob job = Assert.Single(dbContext.ForwardingJobs);
        Assert.Equal(DecisionKind.Accepted, decision.Decision);
        Assert.Equal(publishedFilter.Id, decision.FilterVersionId);
        Assert.Equal(publishedRewrite.Id, decision.RewriteVersionId);
        Assert.NotNull(decision.RewritePreview);
        Assert.Contains("Footer", decision.RewritePreview);
        Assert.Equal(ForwardingJobStatus.Pending, job.Status);
        Assert.Equal(publishedFilter.Id, job.FilterVersionId);
        Assert.Equal(publishedRewrite.Id, job.RewriteVersionId);
    }

    [Fact]
    public async Task RouteMessageAsync_WhenFilterRejects_DoesNotCreateForwardingJobAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramChannel sourceChannel = CreateChannel("Source");
        TelegramChannel targetChannel = CreateChannel("Target");
        FilterSet filterSet = new();
        FilterVersion publishedFilter = new()
        {
            FilterSet = filterSet,
            VersionNumber = 1,
            Status = FilterVersionStatus.Published,
            FilterDefinition = new FilterSetDefinition(
                "Reject text",
                MappingDefaultPolicy.Allow,
                new HasTextCondition(false, "reject")),
        };
        ChannelMapping mapping = new()
        {
            SourceChannel = sourceChannel,
            TargetChannel = targetChannel,
            ActiveFilterSet = filterSet,
            ForwardingMode = ForwardingMode.CopyAsIs,
        };
        StoredMessage message = CreateMessage(sourceChannel.Id, "Visible text");

        await dbContext.AddRangeAsync(sourceChannel, targetChannel, filterSet, publishedFilter, mapping, message);
        await dbContext.SaveChangesAsync();

        MessageRoutingService service = new(dbContext, new FilterEvaluator(), new MessageRewriter());

        await service.RouteMessageAsync(message, mapping);

        MessageDecision decision = Assert.Single(dbContext.MessageDecisions);
        Assert.Equal(DecisionKind.Rejected, decision.Decision);
        Assert.Empty(dbContext.ForwardingJobs);
    }

    [Fact]
    public async Task RouteMessageAsync_WhenNoFilterConfigured_AcceptsByDefault_AndCreatesForwardingJobAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramChannel sourceChannel = CreateChannel("Source");
        TelegramChannel targetChannel = CreateChannel("Target");
        ChannelMapping mapping = new()
        {
            SourceChannel = sourceChannel,
            TargetChannel = targetChannel,
            ForwardingMode = ForwardingMode.CopyAsIs,
        };
        StoredMessage message = CreateMessage(sourceChannel.Id, "Hello world");

        await dbContext.AddRangeAsync(sourceChannel, targetChannel, mapping, message);
        await dbContext.SaveChangesAsync();

        MessageRoutingService service = new(dbContext, new FilterEvaluator(), new MessageRewriter());

        await service.RouteMessageAsync(message, mapping);

        MessageDecision decision = Assert.Single(dbContext.MessageDecisions);
        ForwardingJob job = Assert.Single(dbContext.ForwardingJobs);
        Assert.Equal(DecisionKind.Accepted, decision.Decision);
        Assert.Null(decision.FilterVersionId);
        Assert.Null(decision.RewriteVersionId);
        Assert.Null(decision.RewritePreview);
        Assert.Equal(ForwardingJobStatus.Pending, job.Status);
    }

    [Fact]
    public async Task RouteMessageAsync_WhenFilterAccepts_AndNoRewriteConfigured_CreatesJobWithoutRewritePreviewAsync()
    {
        await using CopyCatDbContext dbContext = CreateDbContext();
        TelegramChannel sourceChannel = CreateChannel("Source");
        TelegramChannel targetChannel = CreateChannel("Target");
        FilterSet filterSet = new();
        FilterVersion publishedFilter = new()
        {
            FilterSet = filterSet,
            VersionNumber = 1,
            Status = FilterVersionStatus.Published,
            FilterDefinition = FilterSetDefinition.AllowAll(),
        };
        ChannelMapping mapping = new()
        {
            SourceChannel = sourceChannel,
            TargetChannel = targetChannel,
            ActiveFilterSet = filterSet,
            ForwardingMode = ForwardingMode.CopyAsIs,
        };
        StoredMessage message = CreateMessage(sourceChannel.Id, "Hello world");

        await dbContext.AddRangeAsync(sourceChannel, targetChannel, filterSet, publishedFilter, mapping, message);
        await dbContext.SaveChangesAsync();

        MessageRoutingService service = new(dbContext, new FilterEvaluator(), new MessageRewriter());

        await service.RouteMessageAsync(message, mapping);

        MessageDecision decision = Assert.Single(dbContext.MessageDecisions);
        ForwardingJob job = Assert.Single(dbContext.ForwardingJobs);
        Assert.Equal(DecisionKind.Accepted, decision.Decision);
        Assert.Equal(publishedFilter.Id, decision.FilterVersionId);
        Assert.Null(decision.RewriteVersionId);
        Assert.Null(decision.RewritePreview);
        Assert.Equal(ForwardingJobStatus.Pending, job.Status);
    }

    private static CopyCatDbContext CreateDbContext()
    {
        DbContextOptions<CopyCatDbContext> options = new DbContextOptionsBuilder<CopyCatDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new CopyCatDbContext(options);
    }

    private static TelegramChannel CreateChannel(string title)
    {
        return new TelegramChannel
        {
            SessionId = Guid.NewGuid(),
            Title = title,
            TelegramChannelId = Random.Shared.NextInt64(1, int.MaxValue),
            AccessHash = Random.Shared.NextInt64(1, int.MaxValue).ToString(),
            ChannelType = TelegramChannelType.BroadcastChannel,
        };
    }

    private static StoredMessage CreateMessage(Guid sourceChannelId, string text)
    {
        return new StoredMessage
        {
            SessionId = Guid.NewGuid(),
            SourceChannelId = sourceChannelId,
            TelegramMessageId = Random.Shared.NextInt64(1, int.MaxValue),
            MessageDate = DateTimeOffset.UtcNow,
            Text = text,
            NormalizedText = text.ToLowerInvariant(),
        };
    }
}
