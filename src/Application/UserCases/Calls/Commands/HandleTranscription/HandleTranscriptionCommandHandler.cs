using MediatR;
using Microsoft.Extensions.Logging;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleTranscription;

internal sealed class HandleTranscriptionCommandHandler : IRequestHandler<HandleTranscriptionCommand, ErrorOr<Success>>
{
    private readonly IVonageService _vonageService;
    private readonly ILogger<HandleTranscriptionCommandHandler> _logger;

    public HandleTranscriptionCommandHandler(
        IVonageService vonageService,
        ILogger<HandleTranscriptionCommandHandler> logger)
    {
        _vonageService = vonageService;
        _logger = logger;
    }

    public async Task<ErrorOr<Success>> Handle(HandleTranscriptionCommand request, CancellationToken cancellationToken)
    {
        var webhookRequest = request.Request;

        _logger.LogInformation("HandleTranscriptionCommandHandler: réception du transcription callback pour la conversation {ConversationUuid}", webhookRequest.ConversationUuid);

        var downloadResult = await _vonageService.DownloadTranscriptionAsync(
            webhookRequest.TranscriptionUrl,
            cancellationToken);

        var transcriptionResult = downloadResult.Value;
        var transcriptText = transcriptionResult.Channels[0].ExtractTranscript();
        _logger.LogInformation("Transcription: {TranscriptText}", transcriptText);

        return Result.Success;
    }

    internal sealed class HandleTranscriptionCommandValidator : AbstractValidator<HandleTranscriptionCommand>
    {
        public HandleTranscriptionCommandValidator()
        {
            RuleFor(x => x.Request.TranscriptionUrl)
                .NotEmpty()
                .WithMessage("L'URL de transcription est requise.");
        }
    }
}