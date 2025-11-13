namespace Vonage_SSW_Workshop.Infrastructure.Vonage;

/// <summary>
/// Configuration settings for Vonage API integration.
/// </summary>
public sealed class WorkshopSettings
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Workshop";

    /// <summary>
    /// The phone number that will initiate the call (must be a Vonage virtual number).
    /// </summary>
    public required string FromNumber { get; init; }

    /// <summary>
    /// The public URL where Vonage will send webhook callbacks (e.g., for recordings and transcriptions).
    /// Must be publicly accessible. For local development, use ngrok or similar tunneling service.
    /// </summary>
    public required string WebhookBaseUrl { get; init; }
}
