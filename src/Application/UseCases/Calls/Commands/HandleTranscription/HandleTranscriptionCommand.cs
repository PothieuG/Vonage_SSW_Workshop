using ErrorOr;
using MediatR;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleTranscription;

public sealed record HandleTranscriptionCommand(TranscriptionCallbackRequest Request) : IRequest<ErrorOr<Success>>;
