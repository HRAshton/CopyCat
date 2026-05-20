using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a source-to-target channel mapping.
/// </summary>
public sealed class ChannelMapping : IHasAuditTimestamps
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the source channel identifier.
    /// </summary>
    public Guid SourceChannelId { get; set; }

    /// <summary>
    /// Gets or sets the source channel.
    /// </summary>
    public TelegramChannel SourceChannel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target channel identifier.
    /// </summary>
    public Guid TargetChannelId { get; set; }

    /// <summary>
    /// Gets or sets the target channel.
    /// </summary>
    public TelegramChannel TargetChannel { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the mapping is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default filter policy.
    /// </summary>
    public MappingDefaultPolicy DefaultPolicy { get; set; } = MappingDefaultPolicy.Allow;

    /// <summary>
    /// Gets or sets the forwarding mode.
    /// </summary>
    public ForwardingMode ForwardingMode { get; set; } = ForwardingMode.CopyWithRewriting;

    /// <summary>
    /// Gets or sets the active filter set identifier.
    /// </summary>
    public Guid? ActiveFilterSetId { get; set; }

    /// <summary>
    /// Gets or sets the active filter set.
    /// </summary>
    public FilterSet? ActiveFilterSet { get; set; }

    /// <summary>
    /// Gets or sets the active rewrite set identifier.
    /// </summary>
    public Guid? ActiveRewriteSetId { get; set; }

    /// <summary>
    /// Gets or sets the active rewrite set.
    /// </summary>
    public RewriteSet? ActiveRewriteSet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether live forwarding is enabled.
    /// </summary>
    public bool LiveForwardingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the configured backfill count.
    /// </summary>
    public int BackfillCount { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
