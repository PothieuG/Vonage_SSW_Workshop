namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface IVonageService
{
    Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<ErrorOr<string>> DownloadTranscriptionAsync(string transcriptionUrl, CancellationToken cancellationToken = default);
}