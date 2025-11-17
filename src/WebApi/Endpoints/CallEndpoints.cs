using MediatR;
using Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleRecording;
using Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleTranscription;
using Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.InitiateCall;
using Vonage_SSW_Workshop.WebApi.Extensions;

namespace Vonage_SSW_Workshop.WebApi.Endpoints;

public static class CallEndpoints
{
    public static void MapCallEndpoints(this WebApplication app)
    {
        var group = app.MapApiGroup("calls");

        group
            .MapPost("/initiate", async (
                ISender sender,
                InitiateCallCommand command,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.Match(
                    callId => TypedResults.Ok(new { CallId = callId }),
                    CustomResult.Problem);
            })
            .WithName("InitiateCall")
            .ProducesPost();

        group
            .MapPost("/recorded", (
                RecordingCallbackRequest request,
                ILogger<Program> logger,
                IServiceScopeFactory serviceScopeFactory) =>
            {
                logger.LogInformation(
                    "Réception du 'Recording webhook' pour la conversation {ConversationUuid}",
                    request.ConversationUuid);

                var command = new HandleRecordingCommand(request);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                        await sender.Send(command, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Tâche de fond du recording en échec: {RecordingUuid}",
                            request.RecordingUuid);
                    }
                }, CancellationToken.None);

                return TypedResults.Ok(new { message = "Recording envoyé en tâche de fond..." });
            })
            .WithName("RecordingCallback")
            .ProducesPost();

        group
            .MapPost("/transcribed", (
                TranscriptionCallbackRequest request,
                ILogger<Program> logger,
                IServiceScopeFactory serviceScopeFactory) =>
            {
                logger.LogInformation(
                    "Réception du 'Transcription webhook' pour la conversation {ConversationUuid}",
                    request.ConversationUuid);

                var command = new HandleTranscriptionCommand(request);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = serviceScopeFactory.CreateScope();
                        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                        await sender.Send(command, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Tâche de fond de la transcription en échec pour le record: {RecordingUuid}",
                            request.RecordingUuid);
                    }
                }, CancellationToken.None);

                return TypedResults.Ok(new { message = "Transcription envoyé en tâche de fond..." });
            })
            .WithName("TranscriptionCallback")
            .ProducesPost();
    }
}