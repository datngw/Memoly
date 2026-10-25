using Memoly.Api.Shared.Domain;

namespace Memoly.Api.Features.System.Ping;

/// <summary>
/// Walking-skeleton handler: proves the endpoint → Wolverine → Result path.
/// It is also the pattern exemplar for real feature slices.
/// </summary>
public static class PingHandler
{
    public static Task<Result<PingResponse>> Handle(PingCommand command, TimeProvider clock, CancellationToken ct)
    {
        var response = new PingResponse(command.Echo, clock.GetUtcNow());
        return Task.FromResult(Result<PingResponse>.Success(response));
    }
}
