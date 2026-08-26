using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Auth;

/// <summary>
/// The single source for "who is calling" — accessed by services and authorization handlers.
/// Read from claims on every request; never instantiated ad hoc.
/// </summary>
public interface ICurrentUser
{
    int UserId { get; }
    string Username { get; }
    Role Role { get; }
    Site Site { get; }
}
