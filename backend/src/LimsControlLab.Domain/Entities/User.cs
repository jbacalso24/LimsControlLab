using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Entities;

/// <summary>
/// Laboratory user account with role and site assignment.
/// Credentials are stored via PasswordHash (salted, never plaintext).
/// </summary>
public sealed class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required Role Role { get; set; }
    public required Site Site { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
