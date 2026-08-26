using LimsControlLab.Api.Auth;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace LimsControlLab.Api.Tests.Auth;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashPasswordReturnsHashedStringNotPlaintext()
    {
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";

        var hash = hasher.HashPassword(null, password);

        Assert.NotEqual(password, hash);
        Assert.NotEmpty(hash);
        Assert.True(hash.Length > 20, "Hash should be sufficiently long (PBKDF2 format)");
    }

    [Fact]
    public void HashPasswordProducesDifferentHashesForSamePassword()
    {
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";

        var hash1 = hasher.HashPassword(null, password);
        var hash2 = hasher.HashPassword(null, password);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyHashedPasswordCorrectPasswordReturnsSuccess()
    {
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";
        var hash = hasher.HashPassword(null, password);

        var result = hasher.VerifyHashedPassword(null, hash, password);

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void VerifyHashedPasswordWrongPasswordReturnsFailed()
    {
        var hasher = new PasswordHasher();
        var password = "TestPassword123!";
        var hash = hasher.HashPassword(null, password);

        var result = hasher.VerifyHashedPassword(null, hash, "WrongPassword");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }
}
