using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Memoly.IntegrationTests.Infrastructure;

namespace Memoly.IntegrationTests.Features.System;

/// <summary>No test-auth override: exercises the real JWT pipeline (no token → 401).</summary>
public sealed class AnonymousApiFactory : MemolyApiFactory
{
    public AnonymousApiFactory()
        : base(useTestAuth: false)
    {
    }
}

public sealed class AuthenticationTests(AnonymousApiFactory factory)
    : IClassFixture<AnonymousApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Post_Ping_WithoutAuthentication_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/system/ping",
            new { echo = "hi" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Health_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_OpenApi_Anonymous_Returns200()
    {
        var response = await _client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
