using ErrorOr;
using MediatR;

namespace Vonage_SSW_Workshop.Application.Common.Interfaces;

public interface ISupabaseStorageService
{
    Task<ErrorOr<Unit>> UploadTextAsync(
        string content,
        string filePath,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Unit>> UploadAudioAsync(
        Stream audioStream,
        string filePath,
        CancellationToken cancellationToken = default);
}