using System.Text.Json.Serialization;

namespace Vonage_SSW_Workshop.Application.UseCases.Calls.Commands.HandleRecording;

public sealed record RecordingCallbackRequest
{
    [JsonPropertyName("conversation_uuid")]
    public required string ConversationUuid { get; init; }

    [JsonPropertyName("recording_uuid")]
    public required string RecordingUuid { get; init; }

    [JsonPropertyName("recording_url")]
    public required string RecordingUrl { get; init; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; init; }

    [JsonPropertyName("end_time")]
    public string? EndTime { get; init; }

    [JsonPropertyName("size")]
    public int? Size { get; init; }
}
