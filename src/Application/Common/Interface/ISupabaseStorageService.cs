using ErrorOr;
using MediatR;

namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface ISupabaseStorageService
{
    Task<ErrorOr<Unit>> UploadTextAsync(
        string content,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Unit>> UploadAudioAsync(
        Stream audioStream,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default);

    string GetPublicUrl(string filePath);
}
