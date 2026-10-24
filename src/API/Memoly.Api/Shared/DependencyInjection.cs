using FluentValidation;

namespace Memoly.Api.Shared;

internal static class DependencyInjection
{
    public static IServiceCollection AddMemoly(this IServiceCollection services, IConfiguration configuration)
    {
        // Feature slices keep their validators internal; registration opts into them.
        services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
