namespace Memoly.Api.Features.System.Ping;

public sealed record PingCommand(string Echo);

public sealed record PingResponse(string Echo, DateTimeOffset PongedAt);
