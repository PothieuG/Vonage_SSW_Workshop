using MediatR;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.InitiateCall;

public sealed record InitiateCallCommand(string CallRequest) : IRequest<ErrorOr<string>>;

internal sealed class InitiateCallCommandHandler(
    IVonageService vonageService)
    : IRequestHandler<InitiateCallCommand, ErrorOr<string>>
{
    public async Task<ErrorOr<string>> Handle(InitiateCallCommand request, CancellationToken cancellationToken) =>
        await vonageService.InitiateCallAsync(request.CallRequest, cancellationToken);
}

internal sealed class InitiateCallCommandValidator : AbstractValidator<InitiateCallCommand>
{
    public InitiateCallCommandValidator()
    {
        RuleFor(v => v.CallRequest)
            .NotEmpty()
            .WithMessage("Un numéro de téléphone est requis.");
    }
}