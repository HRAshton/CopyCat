namespace CopyCat.Application.Exceptions;

/// <summary>
/// Base class for all domain-level application exceptions.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DomainException(string message)
        : base(message)
    {
    }
}
