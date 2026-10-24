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
        IConfiguration configuration)
    {
        // Aspire injects the Keycloak base URL as a connection string; fall back to
        // the appsettings default so plain `dotnet run` also boots.
        var authority = configuration.GetConnectionString("keycloak") is { } keycloak
            ? $"{keycloak}/realms/memoly"
            : configuration["Authentication:Keycloak:Authority"];

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false; // dev is HTTP-only; revisit before production
                options.TokenValidationParameters.ValidateAudience = false; // harden when the SPA login flow lands
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
