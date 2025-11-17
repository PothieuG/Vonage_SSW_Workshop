using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supabase;
using System.Text;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

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
        string filePath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Upload texte vers Supabase: {Path}", filePath);
        var bytes = Encoding.UTF8.GetBytes(content);
        await _supabaseClient.Storage
            .From(_settings.BucketName)
            .Upload(bytes, filePath, new global::Supabase.Storage.FileOptions
            {
                ContentType = "text/plain",
                Upsert = true
            });

        return Unit.Value;
    }

    public async Task<ErrorOr<Unit>> UploadAudioAsync(
        Stream audioStream,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        var audioBytes = memoryStream.ToArray();
        _logger.LogInformation("Upload audio vers Supabase: {Path} ({Size} bytes)", filePath, audioBytes.Length);
        await _supabaseClient.Storage
            .From(_settings.BucketName)
            .Upload(audioBytes, filePath, new global::Supabase.Storage.FileOptions
            {
                ContentType = "audio/mpeg",
                Upsert = true
            });

        return Unit.Value;
    }
}