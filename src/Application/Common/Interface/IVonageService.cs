namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface IVonageService
{
    Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<ErrorOr<string>> DownloadTranscriptionAsync(string transcriptionUrl, CancellationToken cancellationToken = default);
    Task<ErrorOr<CallInfo>> GetCallInfoByConversationUuidAsync(string conversationUuid, CancellationToken cancellationToken = default);
    Task<ErrorOr<string>> SendSmsAsync(CallInfo callInfo, string transcript, string summarizedTranscriptText, CancellationToken cancellationToken = default);
    Task<ErrorOr<Stream>> DownloadRecordingAsync(string recordingUrl, CancellationToken cancellationToken = default);
}

public sealed record CallInfo(string FromNumber, string ToNumber, string DurationSeconds);
