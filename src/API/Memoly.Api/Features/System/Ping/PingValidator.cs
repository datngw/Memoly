using FluentValidation;

namespace Memoly.Api.Features.System.Ping;

internal sealed class PingValidator : AbstractValidator<PingCommand>
{
    public PingValidator()
    {
        RuleFor(x => x.Echo)
            .NotEmpty()
            .MaximumLength(200);
    }
}
