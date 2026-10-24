using FluentAssertions;
using Memoly.Api.Shared.Domain;

namespace Memoly.UnitTests.Shared.Domain;

public class ResultTests
{
    [Fact]
    public void Success_HasValue_IsSuccessWithAccessibleValue()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Failure_HasError_IsFailureWithAccessibleError()
    {
        var error = new Error(ErrorType.NotFound, "not_found", "Nothing there");
        var result = Result<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_OnFailure_ThrowsInvalidOperationException()
    {
        var result = Result<string>.Failure(new Error(ErrorType.Unexpected, "boom", "Something broke"));

        var act = () => result.Value;

        act.Should().ThrowExactly<InvalidOperationException>();
    }

    [Fact]
    public void Error_OnSuccess_ThrowsInvalidOperationException()
    {
        var result = Result<string>.Success("hello");

        var act = () => result.Error;

        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
