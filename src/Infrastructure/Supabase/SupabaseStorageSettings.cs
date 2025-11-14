namespace Vonage_SSW_Workshop.Infrastructure.Supabase;

public sealed class SupabaseStorageSettings
{
    public const string SectionName = "SupabaseStorage";

    public required string ProjectUrl { get; init; }
    public required string ServiceRoleKey { get; init; }
    public required string BucketName { get; init; }
}