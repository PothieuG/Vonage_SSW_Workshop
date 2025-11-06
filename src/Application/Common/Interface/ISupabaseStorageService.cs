using ErrorOr;

namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface ISupabaseStorageService
{
    Task<ErrorOr<string>> UploadTextAsync(
        string content,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<string>> UploadAudioAsync(
        Stream audioStream,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default);
}
