namespace Memoly.Api.Shared.Domain;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Unexpected,
}

public sealed record Error(ErrorType Type, string Code, string Description);
