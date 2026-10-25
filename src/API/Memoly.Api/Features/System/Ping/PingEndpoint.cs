using FluentValidation;
using Memoly.Api.Shared.Domain;
using Memoly.Api.Shared.Presentation;
using Wolverine;

namespace Memoly.Api.Features.System.Ping;

internal static class PingEndpoint
{
    public static RouteGroupBuilder MapPingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/ping", async (
            PingCommand command,
            IMessageBus bus,
            IValidator<PingCommand> validator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
            {
                return TypedResults.ValidationProblem(validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
            }

            var result = await bus.InvokeAsync<Result<PingResponse>>(command, ct);
            return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
        })
        .WithName("Ping")
        .Produces<PingResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        return group;
    }
}
