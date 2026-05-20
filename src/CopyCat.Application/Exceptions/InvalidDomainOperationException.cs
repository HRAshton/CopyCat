namespace CopyCat.Application.Exceptions;

/// <summary>
/// Raised when the caller performs an operation that is not allowed in the current state.
/// </summary>
public sealed class InvalidDomainOperationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDomainOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidDomainOperationException(string message)
        : base(message)
    {
    }
}
