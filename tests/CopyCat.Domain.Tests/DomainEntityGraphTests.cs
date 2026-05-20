using CopyCat.Domain.Entities;
using CopyCat.Domain.Enums;
using CopyCat.Domain.Filters;
using CopyCat.Domain.Rewriting;

namespace CopyCat.Domain.Tests;

public sealed class DomainEntityGraphTests
{
    [Fact]
    public void MappingSnapshot_CanRepresentPublishedRulesAndOperationalState()
    {
        TelegramSession session = new()
        {
            Name = "Primary",
            PhoneNumberMasked = "+1*******90",
            PhoneNumberEncrypted = "enc-phone",
            ApiIdEncrypted = "enc-api-id",
            ApiHashEncrypted = "enc-api-hash",
            PendingPhoneNumber = "+1234567890",
        };
        TelegramChannel sourceChannel = new()
        {
            SessionId = session.Id,
            Session = session,
            TelegramChannelId = 101,
            AccessHash = "source-hash",
            Title = "Source",
            Username = "source_channel",
            ChannelType = TelegramChannelType.BroadcastChannel,
            IsSource = true,
            IsTarget = false,
            CanPost = false,
            CanAdmin = true,
            CanCreateRelatedTargets = true,
            RawJson = "{\"source\":true}",
        };
        TelegramChannel targetChannel = new()
        {
            SessionId = session.Id,
            Session = session,
            TelegramChannelId = 202,
            AccessHash = "target-hash",
            Title = "Target",
            Username = "target_channel",
            ChannelType = TelegramChannelType.MegaGroup,
            IsSource = false,
            IsTarget = true,
            CanPost = true,
            CanAdmin = true,
            CanCreateRelatedTargets = false,
            RawJson = "{\"target\":true}",
        };
        session.Channels = [sourceChannel, targetChannel];

        FilterSet filterSet = new() { Name = "Published filter", Description = "Screens for links" };
        FilterVersion filterVersion = new()
        {
            FilterSetId = filterSet.Id,
            FilterSet = filterSet,
            VersionNumber = 3,
            Status = FilterVersionStatus.Published,
            FilterDefinition = new FilterSetDefinition(
                "Links only",
                MappingDefaultPolicy.Reject,
                new HasTelegramLinkCondition(true, "telegram")),
            CreatedBy = Guid.NewGuid(),
            PublishedAt = DateTimeOffset.UtcNow,
        };
        filterSet.Versions = [filterVersion];

        RewriteSet rewriteSet = new() { Name = "Published rewrite", Description = "Adds a footer" };
        RewriteVersion rewriteVersion = new()
        {
            RewriteSetId = rewriteSet.Id,
            RewriteSet = rewriteSet,
            VersionNumber = 2,
            Status = RewriteVersionStatus.Published,
            Rules = new RewriteRuleSet([new AppendFooterOperation("Footer")]),
            CreatedBy = Guid.NewGuid(),
            PublishedAt = DateTimeOffset.UtcNow,
        };
        rewriteSet.Versions = [rewriteVersion];

        ChannelMapping mapping = new()
        {
            SourceChannelId = sourceChannel.Id,
            SourceChannel = sourceChannel,
            TargetChannelId = targetChannel.Id,
            TargetChannel = targetChannel,
            DefaultPolicy = MappingDefaultPolicy.Reject,
            ForwardingMode = ForwardingMode.CopyWithRewriting,
            ActiveFilterSetId = filterSet.Id,
            ActiveFilterSet = filterSet,
            ActiveRewriteSetId = rewriteSet.Id,
            ActiveRewriteSet = rewriteSet,
            LiveForwardingEnabled = false,
            BackfillCount = 250,
        };

        StoredMessage message = new()
        {
            SessionId = session.Id,
            SourceChannelId = sourceChannel.Id,
            TelegramMessageId = 77,
            GroupedId = "album-77",
            MessageDate = DateTimeOffset.UtcNow.AddMinutes(-5),
            EditDate = DateTimeOffset.UtcNow.AddMinutes(-1),
            Text = "Text",
            Caption = "Caption",
            NormalizedText = "text caption",
            RawJson = "{\"message\":77}",
        };
        MessageAttachment attachment = new()
        {
            MessageId = message.Id,
            Message = message,
            AttachmentType = AttachmentType.Document,
            TelegramFileId = "file-1",
            FileUniqueId = "unique-1",
            MimeType = "application/pdf",
            FileName = "report.pdf",
            SizeBytes = 1024,
            MetadataJson = "{\"pages\":1}",
        };
        MessageLink externalLink = new()
        {
            MessageId = message.Id,
            Message = message,
            Url = "https://example.com",
            LinkType = "External",
            DisplayText = "Example",
            MetadataJson = "{\"host\":\"example.com\"}",
        };
        MessageLink telegramLink = new()
        {
            MessageId = message.Id,
            Message = message,
            Url = "https://t.me/source/77",
            LinkType = "Telegram",
        };
        message.Attachments = [attachment];
        message.Links = [externalLink, telegramLink];

        ChannelSyncState syncState = new()
        {
            SessionId = session.Id,
            ChannelId = sourceChannel.Id,
            LastSeenMessageId = 77,
            LastBackfilledMessageId = 70,
            LastSyncAt = DateTimeOffset.UtcNow,
            SyncStatus = ChannelSyncStatus.Live,
            LastError = "temporary lag",
        };
        ForwardingJob job = new()
        {
            MessageId = message.Id,
            MappingId = mapping.Id,
            FilterVersionId = filterVersion.Id,
            RewriteVersionId = rewriteVersion.Id,
            ForwardingMode = mapping.ForwardingMode,
        };
        job.MarkSucceeded(targetMessageId: 9901);

        MessageDecision decision = new()
        {
            MessageId = message.Id,
            MappingId = mapping.Id,
            FilterVersionId = filterVersion.Id,
            RewriteVersionId = rewriteVersion.Id,
            Decision = DecisionKind.Accepted,
            MatchedRuleId = "telegram",
            TraceJson = "[\"matched telegram link\"]",
            RewritePreview = "Text\n\nFooter",
        };
        AuditLogEntry audit = new()
        {
            ActorUserId = Guid.NewGuid(),
            Action = "mapping.updated",
            EntityType = nameof(ChannelMapping),
            EntityId = mapping.Id,
            BeforeJson = "{\"liveForwardingEnabled\":true}",
            AfterJson = "{\"liveForwardingEnabled\":false}",
        };

        Assert.Equal([sourceChannel, targetChannel], session.Channels);
        Assert.Equal(filterVersion, Assert.Single(filterSet.Versions));
        Assert.Equal(rewriteVersion, Assert.Single(rewriteSet.Versions));
        Assert.Equal(sourceChannel, mapping.SourceChannel);
        Assert.Equal(targetChannel, mapping.TargetChannel);
        Assert.Equal(filterSet, mapping.ActiveFilterSet);
        Assert.Equal(rewriteSet, mapping.ActiveRewriteSet);
        Assert.False(mapping.LiveForwardingEnabled);
        Assert.Equal(250, mapping.BackfillCount);
        Assert.Equal(AttachmentType.Document, Assert.Single(message.Attachments).AttachmentType);
        Assert.Equal(2, message.Links.Count);
        Assert.Equal(ChannelSyncStatus.Live, syncState.SyncStatus);
        Assert.Equal(ForwardingJobStatus.Succeeded, job.Status);
        Assert.Equal(9901, job.TargetTelegramMessageId);
        Assert.Equal(DecisionKind.Accepted, decision.Decision);
        Assert.Equal("telegram", decision.MatchedRuleId);
        Assert.Equal(nameof(ChannelMapping), audit.EntityType);
        Assert.Equal(mapping.Id, audit.EntityId);
    }
}
