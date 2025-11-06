using MediatR;
using Microsoft.Extensions.Logging;
using System.Text;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleTranscription;

internal sealed class HandleTranscriptionCommandHandler : IRequestHandler<HandleTranscriptionCommand, ErrorOr<Success>>
{
    private readonly IVonageService _vonageService;
    private readonly IMcpService _mcpService;
    private readonly ILogger<HandleTranscriptionCommandHandler> _logger;

    public HandleTranscriptionCommandHandler(
        IVonageService vonageService,
        IMcpService mcpService,
        ILogger<HandleTranscriptionCommandHandler> logger)
    {
        _vonageService = vonageService;
        _mcpService = mcpService;
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

        var summarizedTranscriptWitMCPResult = await _mcpService.ProcessTranscriptWithMcpAsync(transcriptText, cancellationToken);
        var summarizedTranscriptText = summarizedTranscriptWitMCPResult.Value;
        _logger.LogInformation("Traitement MCP terminé avec succès - {SummarizedTranscriptText}", summarizedTranscriptText);

        var callInfoObject = await _vonageService.GetCallInfoByConversationUuidAsync(webhookRequest.ConversationUuid, cancellationToken);
        var callInfo = new CallInfo(
            callInfoObject.Value.FromNumber,
            callInfoObject.Value.ToNumber,
            callInfoObject.Value.DurationSeconds);

        var smsResult = await _vonageService.SendSmsAsync(
            callInfo,
            transcriptText,
            summarizedTranscriptText,
            cancellationToken);

        if (smsResult.IsError)
        {
            _logger.LogError("HandleTranscriptionCommandHandler: échec de l'envoie du SMS pour la conversation {ConversationUuid}: {Errors}",
                webhookRequest.ConversationUuid,
                string.Join(", ", smsResult.Errors));
            return smsResult.Errors;
        }

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