namespace CopyCat.Application.Exceptions;

/// <summary>
/// Raised when an operation conflicts with existing state.
/// </summary>
public sealed class DomainConflictException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DomainConflictException(string message)
        : base(message)
    {
    }
}
