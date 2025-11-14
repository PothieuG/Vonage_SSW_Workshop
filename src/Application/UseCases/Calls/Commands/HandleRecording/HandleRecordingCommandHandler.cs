using MediatR;
using Microsoft.Extensions.Logging;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleRecording;

internal sealed class HandleRecordingCommandHandler : IRequestHandler<HandleRecordingCommand, ErrorOr<Success>>
{
    private readonly IVonageService _vonageService;
    private readonly ISupabaseStorageService _supabaseStorage;
    private readonly ILogger<HandleRecordingCommandHandler> _logger;

    public HandleRecordingCommandHandler(
        IVonageService vonageService,
        ISupabaseStorageService supabaseStorage,
        ILogger<HandleRecordingCommandHandler> logger)
    {
        _vonageService = vonageService;
        _supabaseStorage = supabaseStorage;
        _logger = logger;
    }

    public async Task<ErrorOr<Success>> Handle(HandleRecordingCommand request, CancellationToken cancellationToken)
    {
        var webhookRequest = request.Request;
        _logger.LogInformation("HandleRecordingCommandHandler: réception du recording callback pour la conversation {ConversationUuid}",
            webhookRequest.ConversationUuid);
        var recordingStreamResult = await _vonageService.DownloadRecordingAsync(webhookRequest.RecordingUrl, cancellationToken);
        if (recordingStreamResult.IsError)
        {
            _logger.LogError("Échec du téléchargement du recording: {Errors}", string.Join(", ", recordingStreamResult.Errors));
            return recordingStreamResult.Errors;
        }

        await using var recordingStream = recordingStreamResult.Value;
        await _supabaseStorage.UploadAudioAsync(recordingStream, webhookRequest.BuildAudioFilePath(), cancellationToken);
        _logger.LogInformation("Recording uploadé avec succès vers Supabase");
        return Result.Success;
    }
}