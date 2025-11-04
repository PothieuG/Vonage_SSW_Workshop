using SSW_x_Vonage_Clean_Architecture.Application.UseCases.Calls.Commands.HandleTranscription;

namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface IVonageService
{
    Task<string> InitiateCallAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<ErrorOr<TranscriptionResult>> DownloadTranscriptionAsync(string transcriptionUrl, CancellationToken cancellationToken = default);
    Task<ErrorOr<CallInfo>> GetCallInfoByConversationUuidAsync(string conversationUuid, CancellationToken cancellationToken = default);
    Task<ErrorOr<string>> SendSmsAsync(CallInfo callInfo, string transcript, CancellationToken cancellationToken = default);
}

public sealed record CallInfo(string FromNumber, string ToNumber, string DurationSeconds);