using Memoly.Api.Features.System.Ping;

namespace Memoly.Api.Features.System;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/system").WithTags("System");
        group.MapPingEndpoints();
        return app;
    }
}
