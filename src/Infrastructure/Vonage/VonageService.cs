using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using Vonage;
using Vonage.Messages;
using Vonage.Request;
using Vonage.Voice;
using Vonage.Voice.Nccos;
using Vonage.Voice.Nccos.Endpoints;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

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
                        EventUrl = [$"{_settings.WebhookBaseUrl.TrimEnd('/')}/api/calls/transcribed"],
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
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _tokenGenerator.GenerateToken(_credentials).GetSuccessUnsafe()) },
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

    private static Error GetSerializationFailure() =>
        Error.Failure("Vonage.DeserializationFailed", "Echec lors de la désérialisation du JSON de transcription.");
}