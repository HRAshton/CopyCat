namespace CopyCat.Domain.Enums;

/// <summary>
/// The boolean operator used to combine child conditions in a group.
/// </summary>
public enum LogicalOperator
{
    /// <summary>All children must match.</summary>
    And,

    /// <summary>At least one child must match.</summary>
    Or,
}
