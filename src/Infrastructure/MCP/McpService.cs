using ErrorOr;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using Vonage_SSW_Workshop.Application.Common.Interfaces;

namespace Vonage_SSW_Workshop.Infrastructure.MCP;

public class McpService : IMcpService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpService> _logger;
    public McpService(HttpClient httpClient, ILogger<McpService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ErrorOr<string>> ProcessTranscriptWithMcpAsync(string transcript, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Début du traitement MCP du transcript ({Length} caractères)", transcript.Length);

            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new
                {
                    name = "summarize_text",
                    arguments = new
                    {
                        text = transcript
                    }
                }
            };

            _logger.LogInformation("Envoi du transcript au serveur MCP pour traitement complet");
            var response = await _httpClient.PostAsJsonAsync("", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

            var content = jsonResponse.GetProperty("result").GetProperty("content");
            if (content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0)
            {
                var firstContent = content[0];
                var result = firstContent.GetProperty("text").GetString() ?? transcript;
                return result;
            }

            _logger.LogWarning("MCP a retourné une réponse vide, utilisation du transcript original");
            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement MCP du transcript");
            return Error.Failure("Mcp.ProcessingError", ex.Message);
        }
    }
}