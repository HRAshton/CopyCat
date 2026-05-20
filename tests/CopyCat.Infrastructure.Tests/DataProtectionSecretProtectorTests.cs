using CopyCat.Infrastructure.Security;

using Microsoft.AspNetCore.DataProtection;

namespace CopyCat.Infrastructure.Tests;

public sealed class DataProtectionSecretProtectorTests
{
    [Fact]
    public void ProtectAndUnprotect_RoundTripPlaintext()
    {
        IDataProtectionProvider provider = new FakeDataProtectionProvider();
        DataProtectionSecretProtector protector = new(provider.CreateProtector("secrets"));

        string encrypted = protector.Protect("hello");
        string decrypted = protector.Unprotect(encrypted);

        Assert.NotEqual("hello", encrypted);
        Assert.Equal("hello", decrypted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NullableHelpers_ReturnInputForBlankValues(string? value)
    {
        IDataProtectionProvider provider = new FakeDataProtectionProvider();
        DataProtectionSecretProtector protector = new(provider.CreateProtector("secrets"));

        Assert.Equal(value, protector.ProtectNullable(value));
        Assert.Equal(value, protector.UnprotectNullable(value));
    }

    [Theory]
    [InlineData("+1234567890", "+1*******90")]
    [InlineData("1234", "****")]
    public void MaskPhoneNumber_MasksExpectedDigits(string input, string expected)
    {
        IDataProtectionProvider provider = new FakeDataProtectionProvider();
        DataProtectionSecretProtector protector = new(provider.CreateProtector("secrets"));

        Assert.Equal(expected, protector.MaskPhoneNumber(input));
    }

    private sealed class FakeDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return new FakeDataProtector();
        }
    }

    private sealed class FakeDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            return this;
        }

        public byte[] Protect(byte[] plaintext)
        {
            return plaintext.Reverse().ToArray();
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            return protectedData.Reverse().ToArray();
        }
    }
}
