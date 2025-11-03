using Microsoft.Extensions.Options;
using Vonage;
using Vonage.Request;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

internal sealed class VonageService : IVonageService
{
    private readonly VonageClient _vonageClient;
    private readonly VonageSettings _settings;
    private readonly HttpClient _httpClient;

    public VonageService(
        IOptions<VonageSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _httpClient = httpClientFactory.CreateClient();
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
}
