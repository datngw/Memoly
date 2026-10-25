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

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false; // dev is HTTP-only behind the Aspire proxy; revisit before production
                options.TokenValidationParameters.ValidateAudience = false; // harden when the SPA login flow lands
                if (environment.IsDevelopment())
                {
                    // The Aspire DCP proxy terminates TLS with a self-signed certificate;
                    // trust it for OIDC metadata retrieval in development only.
                    options.BackchannelHttpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                    };
                }
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
