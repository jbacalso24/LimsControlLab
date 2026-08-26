using Microsoft.AspNetCore.Identity;

namespace LimsControlLab.Api.Auth;

/// <summary>
/// Password hashing using ASP.NET Core Identity's PasswordHasher with proper salting.
/// Never plaintext, never reversible encryption.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher<object>
{
    private readonly PasswordHasher<object> _inner = new();

    public string HashPassword(object? user, string password) =>
        _inner.HashPassword(new object(), password);

    public PasswordVerificationResult VerifyHashedPassword(object? user, string hashedPassword, string providedPassword) =>
        _inner.VerifyHashedPassword(new object(), hashedPassword, providedPassword);
}
