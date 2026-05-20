using System.Text.Json.Serialization;

namespace CopyCat.Domain.Rewriting;

/// <summary>
/// Represents a single rewrite operation.
/// </summary>
[JsonConverter(typeof(RewriteOperationJsonConverter))]
public abstract record RewriteOperation;
