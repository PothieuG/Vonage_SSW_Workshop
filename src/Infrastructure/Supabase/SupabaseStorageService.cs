using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vonage_SSW_Workshop.Application.Common.Interfaces;
using Supabase;
using System.Text;

namespace Vonage_SSW_Workshop.Infrastructure.Supabase;

internal sealed class SupabaseStorageService : ISupabaseStorageService
{
    private readonly SupabaseStorageSettings _settings;
    private readonly ILogger<SupabaseStorageService> _logger;
    private readonly Client _supabaseClient;

    public SupabaseStorageService(
        IOptions<SupabaseStorageSettings> settings,
        ILogger<SupabaseStorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var options = new SupabaseOptions
        {
            AutoConnectRealtime = false
        };

        _supabaseClient = new Client(_settings.ProjectUrl, _settings.ServiceRoleKey, options);
    }

    public async Task<ErrorOr<Unit>> UploadTextAsync(
        string content,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = $"{folderPath}/{fileName}";

        _logger.LogInformation("Upload texte vers Supabase: {Path}", fullPath);

        var bytes = Encoding.UTF8.GetBytes(content);

        await _supabaseClient.Storage
            .From(_settings.BucketName)
            .Upload(bytes, fullPath, new global::Supabase.Storage.FileOptions
            {
                ContentType = "text/plain",
                Upsert = true
            });

        return Unit.Value;
    }

    public async Task<ErrorOr<Unit>> UploadAudioAsync(
        Stream audioStream,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = $"{folderPath}/{fileName}";

        await using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        var audioBytes = memoryStream.ToArray();

        _logger.LogInformation("Upload audio vers Supabase: {Path} ({Size} bytes)", fullPath, audioBytes.Length);

        await _supabaseClient.Storage
            .From(_settings.BucketName)
            .Upload(audioBytes, fullPath, new global::Supabase.Storage.FileOptions
            {
                ContentType = "audio/mpeg",
                Upsert = true
            });

        return Unit.Value;
    }
}
