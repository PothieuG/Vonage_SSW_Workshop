using System.Text.Json.Nodes;

namespace MCPServer.Services;

public class ToolsService
{
    private readonly OllamaService _ollama;

    public ToolsService(OllamaService ollama)
    {
        _ollama = ollama;
    }

    public async Task<string> Execute(string toolName, JsonNode? arguments)
    {
        return toolName switch
        {
            "summarize_text" => await _ollama.Summarize(
                arguments?["text"]?.ToString() ?? ""),
            _ => throw new Exception($"Outil inconnu: {toolName}")
        };
    }
}