namespace CopyCat.Application.Exceptions;

/// <summary>
/// Raised when a requested entity cannot be found.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">The name of the entity type that was not found.</param>
    /// <param name="id">The identifier of the entity that was not found.</param>
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} '{id}' was not found.")
    {
    }
}
