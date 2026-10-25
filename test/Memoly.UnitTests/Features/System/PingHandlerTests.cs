using FluentAssertions;
using Memoly.Api.Features.System.Ping;
using Microsoft.Extensions.Time.Testing;

namespace Memoly.UnitTests.Features.System;

public class PingHandlerTests
{
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithEcho()
    {
        var result = await PingHandler.Handle(new PingCommand("hello"), _clock, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Echo.Should().Be("hello");
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsPongedAtFromClock()
    {
        var result = await PingHandler.Handle(new PingCommand("hello"), _clock, CancellationToken.None);

        result.Value.PongedAt.Should().Be(_clock.GetUtcNow());
    }
}
