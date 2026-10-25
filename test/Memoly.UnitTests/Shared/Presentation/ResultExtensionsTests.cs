using FluentAssertions;
using Memoly.Api.Shared.Domain;
using Memoly.Api.Shared.Presentation;
using Microsoft.AspNetCore.Http;

namespace Memoly.UnitTests.Shared.Presentation;

public class ResultExtensionsTests
{
    [Fact]
    public void ToProblem_ValidationError_Returns400()
    {
        var result = Result<string>.Failure(new Error(ErrorType.Validation, "validation", "Invalid input"));

        var problem = result.ToProblem();

        ((IStatusCodeHttpResult)problem).StatusCode.Should().Be(400);
    }

    [Fact]
    public void ToProblem_NotFoundError_Returns404()
    {
        var result = Result<string>.Failure(new Error(ErrorType.NotFound, "not_found", "Nothing there"));

        var problem = result.ToProblem();

        ((IStatusCodeHttpResult)problem).StatusCode.Should().Be(404);
    }

    [Fact]
    public void ToProblem_ConflictError_Returns409()
    {
        var result = Result<string>.Failure(new Error(ErrorType.Conflict, "conflict", "Already exists"));

        var problem = result.ToProblem();

        ((IStatusCodeHttpResult)problem).StatusCode.Should().Be(409);
    }

    [Fact]
    public void ToProblem_UnauthorizedError_Returns401()
    {
        var result = Result<string>.Failure(new Error(ErrorType.Unauthorized, "unauthorized", "No access"));

        var problem = result.ToProblem();

        ((IStatusCodeHttpResult)problem).StatusCode.Should().Be(401);
    }

    [Fact]
    public void ToProblem_UnexpectedError_Returns500()
    {
        var result = Result<string>.Failure(new Error(ErrorType.Unexpected, "unexpected", "Something broke"));

        var problem = result.ToProblem();

        ((IStatusCodeHttpResult)problem).StatusCode.Should().Be(500);
    }
}
