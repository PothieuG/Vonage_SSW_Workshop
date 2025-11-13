using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;
using Vonage;
using Vonage.Messages.Sms;
using Vonage.Request;
using Vonage_SSW_Workshop.Application.Common.Interfaces;
using Vonage.Messages;
using Vonage.Voice;
using Vonage.Voice.Nccos;
using Vonage.Voice.Nccos.Endpoints;

namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

internal sealed class VonageService : IVonageService
{
    private readonly WorkshopSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly IMessagesClient _messagesClient;
    private readonly IVoiceClient _voiceClient;
    private readonly ILogger<VonageService> _logger;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly Credentials _credentials;

    public VonageService(
        IOptions<WorkshopSettings> settings,
        IHttpClientFactory httpClientFactory,
        IMessagesClient messagesClient,
        IVoiceClient voiceClient,
        ILogger<VonageService> logger,
        ITokenGenerator tokenGenerator,
        Credentials credentials)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _messagesClient = messagesClient;
        _voiceClient = voiceClient;
        _logger = logger;
        _tokenGenerator = tokenGenerator;
        _credentials = credentials;
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
                EventUrl = [$"{webhookBaseUrl}/api/calls/recorded"],
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

            var response = await _voiceClient.CreateCallAsync(callRequest);

            _logger.LogInformation("VonageService: Appel aura été initié avec un UUID {CallUuid}", response.Uuid);

            return response.Uuid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VonageService: Echec de l'appel vers le numéro {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task<ErrorOr<string>> DownloadTranscriptionAsync(string transcriptionUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Téléchargement de la transcription à l'url {TranscriptionUrl}...", transcriptionUrl);
        
        try
        {
            var response = await GetFromUrlWithCredentials(transcriptionUrl, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var transcription = JsonSerializer.Deserialize<TranscriptionResult>(json);
            if (transcription is null)
            {
                _logger.LogError("Echec lors de la désérialisation du JSON de transcription depuis {TranscriptionUrl}", transcriptionUrl);
                return Error.Failure("Vonage.DeserializationFailed", "Echec lors de la désérialisation du JSON de transcription.");
            }

            _logger.LogInformation("Téléchargement de la transcription réussi depuis {TranscriptionUrl}", transcriptionUrl);

            return transcription.ExtractTranscripts().FirstOrDefault() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VonageService: Erreur lors de la récupération du transcript à l'url - {TranscriptionUrl}", transcriptionUrl);
            return Error.Failure("Vonage.UnexpectedError", $"Erreur innattendu: {ex.Message}");
        }
    }

    private async Task<HttpResponseMessage> GetFromUrlWithCredentials(string transcriptionUrl, CancellationToken cancellationToken) =>
        await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, transcriptionUrl)
        {
            Headers = {Authorization = new AuthenticationHeaderValue("Bearer", _tokenGenerator.GenerateToken(_credentials).GetSuccessUnsafe())},
        }, cancellationToken);

    public async Task<ErrorOr<CallInfo>> GetCallInfoByConversationUuidAsync(string conversationUuid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VonageService: Récupération des informations pour la conversation - {ConversationUuid}", conversationUuid);

        try
        {

            var searchFilter = new CallSearchFilter
            {
                ConversationUuid = conversationUuid
            };

            var callsResponse = await _voiceClient.GetCallsAsync(searchFilter);
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

    public async Task<ErrorOr<string>> SendSmsAsync(CallInfo callInfo, string transcript, string summarizedTranscriptText, string audioUrl, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Envoie d'un SMS au numéro {PhoneNumber}", callInfo.ToNumber);
            var smsMessage = BuildSmsMessage(callInfo.ToNumber, callInfo.DurationSeconds, transcript, summarizedTranscriptText, audioUrl);

            var smsRequest = new SmsRequest
            {
                From = callInfo.FromNumber,
                To = callInfo.ToNumber,
                Text = smsMessage
            };

            var response = await _messagesClient.SendAsync(smsRequest);
            return response.MessageUuid.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoie du SMS vers {ToNumber}", callInfo.ToNumber);
            return Error.Failure("Vonage.SmsSendFailed", $"Erreur lors de l'envoie du SMS: {ex.Message}");
        }
    }
    
    public async Task<ErrorOr<Stream>> DownloadRecordingAsync(string recordingUrl, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VonageService: Téléchargement du recording depuis {RecordingUrl}", recordingUrl);

        var response = await GetFromUrlWithCredentials(recordingUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Échec du téléchargement du recording: Status {StatusCode}", response.StatusCode);
            return Error.Failure("Vonage.DownloadFailed", $"Status: {response.StatusCode}");
        }

        var memoryStream = new MemoryStream();
        await using var httpStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await httpStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        _logger.LogInformation("Recording téléchargé avec succès: {Size} bytes", memoryStream.Length);

        return memoryStream;
    }

    private static string BuildSmsMessage(string fromNumber, string durationSeconds, string transcriptText, string summarizedTranscriptText, string audioUrl)
    {
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("📞 Nouveau message vocal");
        messageBuilder.AppendLine("----------------------");
        messageBuilder.AppendLine($"De: {fromNumber}");
        messageBuilder.AppendLine($"Durée: {durationSeconds}s");
        messageBuilder.AppendLine($"🗒️ Résumé: {summarizedTranscriptText}");
        messageBuilder.AppendLine($"🗒️ Transcription: {transcriptText}");
        messageBuilder.AppendLine($"🎧 Audio: {audioUrl}");
        messageBuilder.AppendLine("----------------------");
        messageBuilder.AppendLine("Bonne journée!");
        return messageBuilder.ToString();
    }
}
