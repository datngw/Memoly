using Memoly.Api.Shared.Domain;
using Microsoft.AspNetCore.Http;

namespace Memoly.Api.Shared.Presentation;

public static class ResultExtensions
{
    /// <summary>
    /// Maps a failed <see cref="Result{T}"/> to an RFC 9457 problem response.
    /// Only call on the failure path — accessing <see cref="Result{T}.Error"/> on success throws.
    /// </summary>
    public static IResult ToProblem<T>(this Result<T> result)
    {
        var error = result.Error;
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            type: $"https://memoly.dev/errors/{error.Code}");
    }
}
