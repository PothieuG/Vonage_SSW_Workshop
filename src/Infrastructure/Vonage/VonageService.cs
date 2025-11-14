using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    public VonageService(
        IOptions<WorkshopSettings> settings,
        IHttpClientFactory httpClientFactory,
        IMessagesClient messagesClient,
        IVoiceClient voiceClient)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
        _messagesClient = messagesClient;
        _voiceClient = voiceClient;
    }

    public async Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        _ = _settings.WebhookBaseUrl.TrimEnd('/');
        var response = await _voiceClient.CreateCallAsync(BuildCallCommand(phoneNumber));
        return response.Uuid;
    }

    private CallCommand BuildCallCommand(string phoneNumber)
    {
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
        return callRequest;
    }
}