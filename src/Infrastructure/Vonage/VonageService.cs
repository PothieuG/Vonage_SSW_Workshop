using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using Vonage;
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
            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, transcriptionUrl)
            {
                Headers = {Authorization = new AuthenticationHeaderValue("Bearer", _tokenGenerator.GenerateToken(_credentials).GetSuccessUnsafe())},
            }, cancellationToken);
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
            _logger.LogError(ex, "VonageService: Unexpected error downloading transcription from {TranscriptionUrl}", transcriptionUrl);
            return Error.Failure("Vonage.UnexpectedError", $"Unexpected error: {ex.Message}");
        }
    }
}
