using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vonage;
using Vonage.Request;
using Vonage.Voice;
using Vonage.Voice.Nccos;
using Vonage.Voice.Nccos.Endpoints;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

internal sealed class VonageService : IVonageService
{
    private readonly VonageClient _vonageClient;
    private readonly VonageSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<VonageService> _logger;

    public VonageService(
        IOptions<VonageSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<VonageService> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;

        var privateKey = GetPrivateKeyContent(_settings.ApplicationKey);
        var credentials = Credentials.FromAppIdAndPrivateKey(
            _settings.ApplicationId,
            privateKey);
        _vonageClient = new VonageClient(credentials);
    }

    private static string GetPrivateKeyContent(string applicationKey)
    {
        if (File.Exists(applicationKey))
            return File.ReadAllText(applicationKey);
        return applicationKey;
    }

    public async Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("VonageService - Appel depuis {FromNumber} vers {PhoneNumber}", _settings.FromNumber, phoneNumber);

        try
        {
            var webhookBaseUrl = _settings.WebhookBaseUrl.TrimEnd('/');

            var ncco = new Ncco(
                new TalkAction
                {
                    Text = "Bonjour, veuillez laisser un message après le bip svp.",
                    Language = "fr-FR",
                    Style = 0
                }
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
}
