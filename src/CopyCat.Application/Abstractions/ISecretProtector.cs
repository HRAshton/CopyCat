namespace CopyCat.Application.Abstractions;

/// <summary>
/// Protects and restores sensitive values used by the application.
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// Encrypts or otherwise protects a plaintext value.
    /// </summary>
    /// <param name="plaintext">The value to protect.</param>
    /// <returns>The protected (encrypted) representation.</returns>
    string Protect(string plaintext);

    /// <summary>
    /// Protects a nullable plaintext value.
    /// </summary>
    /// <param name="plaintext">The value to protect, or <c>null</c>.</param>
    /// <returns>The protected value, or <c>null</c> if the input was <c>null</c>.</returns>
    string? ProtectNullable(string? plaintext);

    /// <summary>
    /// Restores a protected value back to plaintext.
    /// </summary>
    /// <param name="protectedValue">The protected value to unprotect.</param>
    /// <returns>The original plaintext.</returns>
    string Unprotect(string protectedValue);

    /// <summary>
    /// Restores a nullable protected value back to plaintext.
    /// </summary>
    /// <param name="protectedValue">The protected value to unprotect, or <c>null</c>.</param>
    /// <returns>The original plaintext, or <c>null</c> if the input was <c>null</c>.</returns>
    string? UnprotectNullable(string? protectedValue);

    /// <summary>
    /// Masks a phone number for UI display.
    /// </summary>
    /// <param name="phoneNumber">The phone number to mask.</param>
    /// <returns>A partially masked representation of the phone number.</returns>
    string MaskPhoneNumber(string phoneNumber);
}
