using CopyCat.Domain.Enums;

namespace CopyCat.Domain.Filters;

/// <summary>
/// Represents a condition that checks message attachments.
/// </summary>
public sealed record HasAttachmentCondition(
    IReadOnlyList<AttachmentType> Types,
    string? RuleId = null) : FilterNode(RuleId);
