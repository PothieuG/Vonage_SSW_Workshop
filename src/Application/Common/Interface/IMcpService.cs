namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface IMcpService
{
    Task<ErrorOr<string>> ProcessTranscriptWithMcpAsync(string transcript, CancellationToken cancellationToken = default);
}