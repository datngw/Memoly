using FluentAssertions;
using Memoly.Api.Features.System.Ping;

namespace Memoly.UnitTests.Features.System;

public class PingValidatorTests
{
    private readonly PingValidator _validator = new();

    [Fact]
    public void Validate_EmptyEcho_Fails()
    {
        _validator.Validate(new PingCommand("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EchoOver200Chars_Fails()
    {
        _validator.Validate(new PingCommand(new string('a', 201))).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShortEcho_Succeeds()
    {
        _validator.Validate(new PingCommand("hi")).IsValid.Should().BeTrue();
    }
}
