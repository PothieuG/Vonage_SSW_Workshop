using OllamaSharp;

namespace MCPServer.Services;

public class OllamaService
{
    private readonly OllamaApiClient _client;

    public OllamaService(string url, string model)
    {
        _client = new OllamaApiClient(url, model);
    }
}
