using MediatR;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleRecording;

public sealed record HandleRecordingCommand(RecordingCallbackRequest Request) : IRequest<ErrorOr<Success>>;
