namespace Memoly.Api.Shared.Domain;

/// <summary>
/// Result of an operation that can fail without throwing: either a value or an <see cref="Error"/>.
/// Accessing the wrong side throws — check <see cref="IsSuccess"/> first.
/// </summary>
public sealed record Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error of a successful result.");

    public static Result<T> Success(T value) => new(isSuccess: true, value, error: null);

    public static Result<T> Failure(Error error) => new(isSuccess: false, value: default, error);
}
