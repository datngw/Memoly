using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Memoly.Api.Shared.Auth;

internal static class DependencyInjection
{
    /// <summary>
    /// Keycloak-backed JWT authentication with a fallback policy that requires an
    /// authenticated user on every endpoint unless it opts out with AllowAnonymous.
    /// </summary>
    public static IServiceCollection AddMemolyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Aspire's Keycloak integration publishes the endpoint via service discovery:
        // env "services__keycloak__https__0" maps to the hierarchical config key below.
        // A plain connection string would win if the integration ever provides one;
        // appsettings is the last fallback so plain `dotnet run` also boots.
        var keycloakBase = configuration.GetConnectionString("keycloak")
            ?? configuration["services:keycloak:https:0"];

        var authority = keycloakBase is { } baseUrl
            ? $"{baseUrl.TrimEnd('/')}/realms/memoly"
            : configuration["Authentication:Keycloak:Authority"];

        var isDevelopment = environment.IsDevelopment();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                if (isDevelopment)
                {
                    // Dev relaxations, Development-gated so they cannot ship: plain HTTP behind
                    // the Aspire DCP proxy (which also terminates TLS with a self-signed cert,
                    // trusted below), and no audience check until the SPA login flow defines one.
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidateAudience = false;
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                    };
                }
                // Outside Development: defaults require HTTPS metadata and audience validation,
                // failing closed until a production identity setup is configured.
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}
