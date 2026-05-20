using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Entities;

/// <summary>
/// Represents a filter and rewrite decision for a message.
/// </summary>
public sealed class MessageDecision
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the message identifier.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the mapping identifier.
    /// </summary>
    public Guid MappingId { get; set; }

    /// <summary>
    /// Gets or sets the filter version identifier.
    /// </summary>
    public Guid? FilterVersionId { get; set; }

    /// <summary>
    /// Gets or sets the rewrite version identifier.
    /// </summary>
    public Guid? RewriteVersionId { get; set; }

    /// <summary>
    /// Gets or sets the decision kind.
    /// </summary>
    public DecisionKind Decision { get; set; }

    /// <summary>
    /// Gets or sets the matched rule identifier.
    /// </summary>
    public string? MatchedRuleId { get; set; }

    /// <summary>
    /// Gets or sets the trace JSON.
    /// </summary>
    public string? TraceJson { get; set; }

    /// <summary>
    /// Gets or sets the rewrite preview.
    /// </summary>
    public string? RewritePreview { get; set; }

    /// <summary>
    /// Gets or sets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
