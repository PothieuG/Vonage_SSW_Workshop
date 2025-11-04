using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SSW_x_Vonage_Clean_Architecture.Application.UseCases.Calls.Commands.HandleTranscription;
using System.Text;
using System.Text.Json;
using Vonage;
using Vonage.Messages.Sms;
using Vonage.Voice;
using Vonage.Voice.Nccos;
using Vonage.Voice.Nccos.Endpoints;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

internal sealed class VonageService : IVonageService
{
    private readonly VonageClient _vonageClient;
    private readonly VonageSettings _settings;
    private readonly IVonageAuthenticatedHttpClient _authenticatedHttpClient;
    private readonly ILogger<VonageService> _logger;

    public VonageService(
        IOptions<VonageSettings> settings,
        VonageClient vonageClient,
        IVonageAuthenticatedHttpClient authenticatedHttpClient,
        ILogger<VonageService> logger)
    {
        _settings = settings.Value;
        _vonageClient = vonageClient;
        _authenticatedHttpClient = authenticatedHttpClient;
        _logger = logger;
    }

    public async Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VonageService - Appel depuis {FromNumber} vers {PhoneNumber}", _settings.FromNumber, phoneNumber);

        try
        {
            var webhookBaseUrl = _settings.WebhookBaseUrl.TrimEnd('/');

            var recordAction = new RecordAction
            {
                EndOnSilence = "3",
                BeepStart = true,
                Transcription = new RecordAction.TranscriptionSettings
                {
                    EventUrl = [$"{webhookBaseUrl}/api/calls/transcribed"],
                    Language = "fr-FR"
                }
            };

            var ncco = new Ncco(
                new TalkAction
                {
                    Text = "Bonjour, veuillez laisser un message après le bip svp.",
                    Language = "fr-FR",
                    Style = 0
                },
                recordAction
            );

            var callRequest = new CallCommand
            {
                To = [new PhoneEndpoint { Number = phoneNumber }],
                From = new PhoneEndpoint { Number = _settings.FromNumber },
                Ncco = ncco
            };

            var response = await _vonageClient.VoiceClient.CreateCallAsync(callRequest);

            _logger.LogInformation("VonageService: Appel aura été initié avec un UUID {CallUuid}", response.Uuid);

            return response.Uuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VonageService: Echec de l'appel vers le numéro {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<ErrorOr<TranscriptionResult>> DownloadTranscriptionAsync(string transcriptionUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Téléchargement de la transcription à l'url {TranscriptionUrl}...", transcriptionUrl);

        var responseResult = await _authenticatedHttpClient.GetAuthenticatedAsync(transcriptionUrl, cancellationToken);

        try
        {
            var response = responseResult.Value;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var transcription = JsonSerializer.Deserialize<TranscriptionResult>(json);

            if (transcription is null)
            {
                _logger.LogError("Echec lors de la désérialisation du JSON de transcription depuis {TranscriptionUrl}", transcriptionUrl);
                return Error.Failure("Vonage.DeserializationFailed", "Echec lors de la désérialisation du JSON de transcription.");
            }

            _logger.LogInformation("Téléchargement de la transcription réussi depuis {TranscriptionUrl}", transcriptionUrl);

            return transcription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VonageService: Erreur lors de la récupération du transcript à l'url - {TranscriptionUrl}", transcriptionUrl);
            return Error.Failure("Vonage.UnexpectedError", $"Erreur innattendu: {ex.Message}");
        }
    }

    public async Task<ErrorOr<CallInfo>> GetCallInfoByConversationUuidAsync(string conversationUuid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VonageService: Récupération des informations pour la conversation - {ConversationUuid}", conversationUuid);

        try
        {

            var searchFilter = new CallSearchFilter
            {
                ConversationUuid = conversationUuid
            };

            var callsResponse = await _vonageClient.VoiceClient.GetCallsAsync(searchFilter);
            var call = callsResponse.Embedded.Calls.FirstOrDefault();

            if (call is null)
            {
                _logger.LogWarning(
                    "VonageService: Aucun appel trouvé pour la conversation {ConversationUuid}",
                    conversationUuid);

                return Error.Failure(
                    "Vonage.CallNotFound",
                    "Aucun enregistrement d'appel trouvé pour cette conversation UUID");
            }

            var callInfo = new CallInfo(call.From.Number, call.To.Number, call.Duration);

            return callInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récuparation des informations de la conversation {ConversationUuid}", conversationUuid);
            return Error.Failure("Vonage.GetCallFailed", $"Erreur lors de la récupération des informations de la conversation: {ex.Message}");
        }
    }

    public async Task<ErrorOr<string>> SendSmsAsync(CallInfo callInfo, string transcript, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Envoie d'un SMS au numéro {PhoneNumber}", callInfo.ToNumber);
            var smsMessage = BuildSmsMessage(callInfo.ToNumber, callInfo.DurationSeconds, transcript);

            var smsRequest = new SmsRequest
            {
                From = callInfo.FromNumber,
                To = callInfo.ToNumber,
                Text = smsMessage
            };

            var messagesClient = _vonageClient.MessagesClient;
            var response = await messagesClient.SendAsync(smsRequest);
            return response.MessageUuid.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoie du SMS vers {ToNumber}", callInfo.ToNumber);
            return Error.Failure("Vonage.SmsSendFailed", $"Erreur lors de l'envoie du SMS: {ex.Message}");
        }
    }
    
    private static string BuildSmsMessage(string fromNumber, string durationSeconds, string transcriptText)
    {
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("📞 Nouveau message vocal");
        messageBuilder.AppendLine("----------------------");
        messageBuilder.AppendLine($"De: {fromNumber}");
        messageBuilder.AppendLine($"Durée: {durationSeconds}s");
        messageBuilder.AppendLine($"🗒️ Transcription: {transcriptText}");
        messageBuilder.AppendLine("----------------------");
        messageBuilder.AppendLine("Bonne journée!");
        return messageBuilder.ToString();
    }
}
