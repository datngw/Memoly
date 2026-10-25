using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Memoly.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against a throwaway Postgres container. With <c>useTestAuth</c>
/// (default) authentication is replaced by <see cref="TestAuthHandler"/> so tests run
/// without Keycloak; pass <c>false</c> to exercise the real auth pipeline (Task 8).
/// </summary>
public class MemolyApiFactory(bool useTestAuth = true)
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Read lazily at host build, i.e. after InitializeAsync started the container.
                ["ConnectionStrings:memoly"] = _db.GetConnectionString(),
            }));

        if (useTestAuth)
        {
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        }
    }

    // xunit v3 IAsyncLifetime uses ValueTask.
    public ValueTask InitializeAsync() => new(_db.StartAsync());

    public async override ValueTask DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
