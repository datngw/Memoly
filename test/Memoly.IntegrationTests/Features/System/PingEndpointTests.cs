using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Memoly.IntegrationTests.Infrastructure;

namespace Memoly.IntegrationTests.Features.System;

/// <summary>Parameterless subclass so xunit can create it as a class fixture (test auth on).</summary>
public sealed class AuthedApiFactory : MemolyApiFactory;

public sealed class PingEndpointTests(AuthedApiFactory factory)
    : IClassFixture<AuthedApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_Ping_ValidBody_Returns200AndEcho()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/system/ping",
            new { echo = "hello" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PingBody>(TestContext.Current.CancellationToken);
        body!.Echo.Should().Be("hello");
    }

    [Fact]
    public async Task Post_Ping_InvalidBody_Returns400ValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/system/ping",
            new { echo = new string('a', 201) },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    private sealed record PingBody(string Echo, DateTimeOffset PongedAt);
}
