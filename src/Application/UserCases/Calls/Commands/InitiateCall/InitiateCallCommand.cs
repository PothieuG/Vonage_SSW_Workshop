using MediatR;
using Microsoft.Extensions.Logging;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.InitiateCall;

public sealed record InitiateCallCommand(string CallRequest) : IRequest<ErrorOr<string>>;

internal sealed class InitiateCallCommandHandler(
    IVonageService vonageService,
    ILogger<InitiateCallCommandHandler> logger)
    : IRequestHandler<InitiateCallCommand, ErrorOr<string>>
{
    public async Task<ErrorOr<string>> Handle(InitiateCallCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("InitiateCallCommand: appel vers le numéro {PhoneNumber}", request.CallRequest);
        var callId = await vonageService.InitiateCallAsync(request.CallRequest, cancellationToken);
        logger.LogInformation("InitiateCallCommand: appel avec succès avec ID {CallId}", callId);
        return callId;
    }
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
