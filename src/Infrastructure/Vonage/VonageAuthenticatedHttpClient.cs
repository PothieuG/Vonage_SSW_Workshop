using ErrorOr;
using Microsoft.Extensions.Logging;
using Vonage;
using Vonage.Request;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

internal sealed class VonageAuthenticatedHttpClient : IVonageAuthenticatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly Credentials _credentials;
    private readonly ILogger<VonageAuthenticatedHttpClient> _logger;

    public VonageAuthenticatedHttpClient(
        IHttpClientFactory httpClientFactory,
        VonageClient vonageClient,
        ILogger<VonageAuthenticatedHttpClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _credentials = vonageClient.Credentials;
        _logger = logger;
    }

    public async Task<ErrorOr<HttpResponseMessage>> GetAuthenticatedAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var jwt = new Jwt();
            var jwtTokenResult = jwt.GenerateToken(_credentials);

            var token = jwtTokenResult.Match(
                success => success,
                failure => throw new InvalidOperationException("Échec de la génération du token JWT"));

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");

            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur inattendue lors de la requête HTTP vers {Url}", url);
            return Error.Failure(
                "VonageHttp.UnexpectedError",
                $"Erreur inattendue: {ex.Message}");
        }
    }
}
