using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Net.Http.Headers;
using Vonage;
using Vonage.Messages.Sms;
using Vonage.Request;
using Vonage_SSW_Workshop.Application.Common.Interfaces;
using Vonage.Common;
using Vonage.Messages;
using Vonage.Voice;
using Vonage.Voice.Nccos;
using Vonage.Voice.Nccos.Endpoints;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

internal sealed class VonageService : IVonageService
{
    private readonly WorkshopSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly IMessagesClient _messagesClient;
    private readonly IVoiceClient _voiceClient;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly Credentials _credentials;

    public VonageService(
        IOptions<WorkshopSettings> settings,
        IHttpClientFactory httpClientFactory,
        IMessagesClient messagesClient,
        IVoiceClient voiceClient,
        ITokenGenerator tokenGenerator,
        Credentials credentials)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _messagesClient = messagesClient;
        _voiceClient = voiceClient;
        _tokenGenerator = tokenGenerator;
        _credentials = credentials;
    }
    
    public async Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var response = await _voiceClient.CreateCallAsync(BuildCallCommand(phoneNumber));
        return response.Uuid;
    }

    private CallCommand BuildCallCommand(string phoneNumber) =>
        new()
        {
            To = [new PhoneEndpoint { Number = phoneNumber }],
            From = new PhoneEndpoint { Number = _settings.FromNumber },
            Ncco = new Ncco(
                new TalkAction
                {
                    Text = "Bonjour, veuillez laisser un message après le bip svp.",
                    Language = "fr-FR",
                    Style = 0
                },
                new RecordAction
                {
                    EndOnSilence = "3",
                    BeepStart = true,
                    Transcription = new RecordAction.TranscriptionSettings
                    {
                        EventUrl = [$"{ _settings.WebhookBaseUrl.TrimEnd('/')}/api/calls/transcribed"],
                        Language = "fr-FR"
                    }
                }
            )
        };

    public async Task<ErrorOr<string>> DownloadTranscriptionAsync(string transcriptionUrl, CancellationToken cancellationToken = default) => 
        await DownloadTranscript(transcriptionUrl, cancellationToken).Then(DeserializeTranscript);

    private async Task<ErrorOr<string>> DownloadTranscript(string transcriptionUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, transcriptionUrl)
            {
                Headers = {Authorization = new AuthenticationHeaderValue("Bearer", _tokenGenerator.GenerateToken(_credentials).GetSuccessUnsafe())},
            }, cancellationToken);
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return Error.Failure("Vonage.HttpRequestFailed", "Echec lors de l'envoi d'une requête HTTP.");
        }
    }

    private static ErrorOr<string> DeserializeTranscript(string json)
    {
        try
        {
            var transcription = JsonSerializer.Deserialize<TranscriptionResult>(json);
            return transcription?.ExtractTranscripts().FirstOrDefault() ?? (ErrorOr<string>)GetSerializationFailure();
        }
        catch
        {
          return GetSerializationFailure();
        }
    }

    public async Task<ErrorOr<CallInfo>> GetCallInfoByConversationUuidAsync(string conversationUuid, CancellationToken cancellationToken = default) => 
        await GetCalls(conversationUuid).Then(FindCall).Then(call => call.ToCallInfo());

    private async Task<ErrorOr<PageResponse<CallList>>> GetCalls(string conversationUuid)
    {
        try
        {
            return await _voiceClient.GetCallsAsync(new CallSearchFilter { ConversationUuid = conversationUuid });
        }
        catch (Exception ex)
        {
            return Error.Failure("Vonage.GetCallFailed", $"Erreur lors de la récupération des informations de la conversation: {ex.Message}");
        }
    }

    private ErrorOr<CallRecord> FindCall(PageResponse<CallList> calls) =>
        calls.Embedded.Calls.FirstOrDefault() ?? (ErrorOr<CallRecord>)Error.Failure(
            "Vonage.CallNotFound",
            "Aucun enregistrement d'appel trouvé pour cette conversation UUID");

    public async Task<ErrorOr<string>> SendSmsAsync(CallInfo callInfo, string transcript, CancellationToken cancellationToken)
    {
        try
        {
            var smsMessage = BuildSmsMessage(callInfo.ToNumber, callInfo.DurationSeconds, transcript);
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
            return Error.Failure("Vonage.SmsSendFailed", $"Erreur lors de l'envoie du SMS: {ex.Message}");
        }
    }

    private static Error GetSerializationFailure() => 
        Error.Failure("Vonage.DeserializationFailed", "Echec lors de la désérialisation du JSON de transcription.");
    
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

public static class CallRecordExtensions
{
    public static CallInfo ToCallInfo(this CallRecord call) => new CallInfo(call.From.Number, call.To.Number, call.Duration);
}