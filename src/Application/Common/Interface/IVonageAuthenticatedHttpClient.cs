namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface IVonageAuthenticatedHttpClient
{
    Task<ErrorOr<HttpResponseMessage>> GetAuthenticatedAsync(string url, CancellationToken cancellationToken = default);
}
