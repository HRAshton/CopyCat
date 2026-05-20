using CopyCat.Domain;
using CopyCat.Domain.Entities;

using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CopyCat.Infrastructure.Data;

/// <summary>
/// Primary EF Core database context for the CopyCat application.
/// </summary>
public sealed class CopyCatDbContext(DbContextOptions<CopyCatDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    /// <summary>
    /// Gets the set of Telegram sessions.
    /// </summary>
    public DbSet<TelegramSession> TelegramSessions => Set<TelegramSession>();

    /// <summary>
    /// Gets the set of Telegram channels discovered for each session.
    /// </summary>
    public DbSet<TelegramChannel> TelegramChannels => Set<TelegramChannel>();

    /// <summary>
    /// Gets the set of source-to-target channel mappings.
    /// </summary>
    public DbSet<ChannelMapping> ChannelMappings => Set<ChannelMapping>();

    /// <summary>
    /// Gets the set of per-channel synchronisation state records.
    /// </summary>
    public DbSet<ChannelSyncState> ChannelSyncStates => Set<ChannelSyncState>();

    /// <summary>
    /// Gets the set of stored Telegram messages.
    /// </summary>
    public DbSet<StoredMessage> Messages => Set<StoredMessage>();

    /// <summary>
    /// Gets the set of message attachments.
    /// </summary>
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();

    /// <summary>
    /// Gets the set of message links extracted from message text.
    /// </summary>
    public DbSet<MessageLink> MessageLinks => Set<MessageLink>();

    /// <summary>
    /// Gets the set of named filter sets.
    /// </summary>
    public DbSet<FilterSet> FilterSets => Set<FilterSet>();

    /// <summary>
    /// Gets the set of versioned filter definitions.
    /// </summary>
    public DbSet<FilterVersion> FilterVersions => Set<FilterVersion>();

    /// <summary>
    /// Gets the set of named rewrite sets.
    /// </summary>
    public DbSet<RewriteSet> RewriteSets => Set<RewriteSet>();

    /// <summary>
    /// Gets the set of versioned rewrite rule definitions.
    /// </summary>
    public DbSet<RewriteVersion> RewriteVersions => Set<RewriteVersion>();

    /// <summary>
    /// Gets the set of per-message forwarding decisions.
    /// </summary>
    public DbSet<MessageDecision> MessageDecisions => Set<MessageDecision>();

    /// <summary>
    /// Gets the set of queued forwarding jobs.
    /// </summary>
    public DbSet<ForwardingJob> ForwardingJobs => Set<ForwardingJob>();

    /// <summary>
    /// Gets the set of queued Telegram control operations (channel discovery, backfill, etc.).
    /// </summary>
    public DbSet<TelegramControlOperation> TelegramControlOperations => Set<TelegramControlOperation>();

    /// <summary>
    /// Gets the set of audit log entries.
    /// </summary>
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();

    /// <summary>
    /// Gets the set used by ASP.NET Core Data Protection to persist encryption keys.
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>
    /// Automatically stamps <see cref="IHasAuditTimestamps.UpdatedAt"/> on every modified or added entity.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used while flushing changes to the database.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.State is not EntityState.Added and not EntityState.Modified)
            {
                continue;
            }

            if (entry.Entity is IHasAuditTimestamps stamped)
            {
                stamped.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Configures the entity model for the application.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelegramSession>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TelegramChannel>().Property(x => x.ChannelType).HasConversion<string>();
        modelBuilder.Entity<TelegramChannel>().HasIndex(x => new { x.SessionId, x.TelegramChannelId }).IsUnique();

        modelBuilder.Entity<ChannelMapping>().Property(x => x.DefaultPolicy).HasConversion<string>();
        modelBuilder.Entity<ChannelMapping>().Property(x => x.ForwardingMode).HasConversion<string>();
        modelBuilder.Entity<ChannelMapping>().HasIndex(x => new { x.SourceChannelId, x.TargetChannelId }).IsUnique();
        modelBuilder.Entity<ChannelMapping>().HasOne(x => x.SourceChannel).WithMany()
            .HasForeignKey(x => x.SourceChannelId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ChannelMapping>().HasOne(x => x.TargetChannel).WithMany()
            .HasForeignKey(x => x.TargetChannelId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChannelSyncState>().Property(x => x.SyncStatus).HasConversion<string>();
        modelBuilder.Entity<ChannelSyncState>().HasIndex(x => new { x.SessionId, x.ChannelId }).IsUnique();
        modelBuilder.Entity<StoredMessage>().HasIndex(x => new { x.SourceChannelId, x.TelegramMessageId }).IsUnique();
        modelBuilder.Entity<MessageAttachment>().Property(x => x.AttachmentType).HasConversion<string>();
        modelBuilder.Entity<FilterVersion>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<FilterVersion>()
            .Property(x => x.FilterDefinition)
            .HasConversion(CopyCatJsonValueConverters.CreateFilterSetDefinitionConverter());
        modelBuilder.Entity<FilterVersion>().HasIndex(x => new { x.FilterSetId, x.VersionNumber }).IsUnique();
        modelBuilder.Entity<RewriteVersion>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<RewriteVersion>()
            .Property(x => x.Rules)
            .HasConversion(CopyCatJsonValueConverters.CreateRewriteRuleSetConverter());
        modelBuilder.Entity<RewriteVersion>().HasIndex(x => new { x.RewriteSetId, x.VersionNumber }).IsUnique();
        modelBuilder.Entity<MessageDecision>().Property(x => x.Decision).HasConversion<string>();
        modelBuilder.Entity<MessageDecision>().HasIndex(x => new { x.MessageId, x.MappingId }).IsUnique();
        modelBuilder.Entity<ForwardingJob>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<ForwardingJob>().Property(x => x.ForwardingMode).HasConversion<string>();
        modelBuilder.Entity<ForwardingJob>().HasIndex(x => new { x.Status, x.NextRetryAt });
        modelBuilder.Entity<ForwardingJob>().HasIndex(x => new { x.MessageId, x.MappingId }).IsUnique();
        modelBuilder.Entity<TelegramControlOperation>().Property(x => x.OperationType).HasConversion<string>();
        modelBuilder.Entity<TelegramControlOperation>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<TelegramControlOperation>().HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
