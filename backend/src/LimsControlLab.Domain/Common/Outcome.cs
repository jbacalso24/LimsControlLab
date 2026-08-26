namespace LimsControlLab.Domain.Common;

public abstract record Outcome
{
    public sealed record Ok<T>(T Data) : Outcome;
    public sealed record NotFound(string Message) : Outcome;
    public sealed record Invalid(string Field, string Message) : Outcome;
    public sealed record Unauthorized(string Message) : Outcome;
    public sealed record Forbidden(string Message) : Outcome;
    public sealed record Conflict(string Message, string? CurrentRowVersion = null) : Outcome;
}

public abstract record Outcome<T>
{
    public sealed record Ok(T Data) : Outcome<T>;
    public sealed record NotFound(string Message) : Outcome<T>;
    public sealed record Invalid(string Field, string Message) : Outcome<T>;
    public sealed record Unauthorized(string Message) : Outcome<T>;
    public sealed record Forbidden(string Message) : Outcome<T>;
    public sealed record Conflict(string Message, string? CurrentRowVersion = null) : Outcome<T>;
}
