using CopyCat.Application.Abstractions;

using Microsoft.AspNetCore.DataProtection;

namespace CopyCat.Infrastructure.Security;

/// <summary>
/// Protects secrets with ASP.NET Core Data Protection and provides safe display helpers for sensitive values.
/// </summary>
public sealed class DataProtectionSecretProtector(IDataProtector protector) : ISecretProtector
{
    /// <summary>
    /// Encrypts a required secret value for persistence.
    /// </summary>
    /// <param name="plaintext">The plaintext secret to protect.</param>
    /// <returns>The protected representation of <paramref name="plaintext"/>.</returns>
    public string Protect(string plaintext)
    {
        return protector.Protect(plaintext);
    }

    /// <summary>
    /// Encrypts an optional secret value when it contains data.
    /// </summary>
    /// <param name="plaintext">The plaintext secret to protect, or <see langword="null"/>/whitespace to leave unchanged.</param>
    /// <returns>The protected representation of <paramref name="plaintext"/>, or the original value when it is blank.</returns>
    public string? ProtectNullable(string? plaintext)
    {
        return string.IsNullOrWhiteSpace(plaintext) ? plaintext : protector.Protect(plaintext);
    }

    /// <summary>
    /// Decrypts a required protected secret value.
    /// </summary>
    /// <param name="protectedValue">The protected secret value to unprotect.</param>
    /// <returns>The decrypted plaintext value.</returns>
    public string Unprotect(string protectedValue)
    {
        return protector.Unprotect(protectedValue);
    }

    /// <summary>
    /// Decrypts an optional protected secret value when it contains data.
    /// </summary>
    /// <param name="protectedValue">The protected secret value to unprotect, or <see langword="null"/>/whitespace to leave unchanged.</param>
    /// <returns>The decrypted plaintext value, or the original value when it is blank.</returns>
    public string? UnprotectNullable(string? protectedValue)
    {
        return string.IsNullOrWhiteSpace(protectedValue) ? protectedValue : protector.Unprotect(protectedValue);
    }

    /// <summary>
    /// Masks the middle digits of a phone number before it is shown in logs or UI.
    /// </summary>
    /// <param name="phoneNumber">The phone number to redact.</param>
    /// <returns>A masked phone number that preserves only the first and last two characters when possible.</returns>
    public string MaskPhoneNumber(string phoneNumber)
    {
        if (phoneNumber.Length <= 4)
        {
            return new string('*', phoneNumber.Length);
        }

        return $"{phoneNumber[..2]}{new string('*', Math.Max(0, phoneNumber.Length - 4))}{phoneNumber[^2..]}";
    }
}
