using MCPServer.Services;
using System.Text.Json.Nodes;

namespace MCPServer.Transport;

public class StdioTransport
{
    private readonly JsonRpcService _jsonRpc;

    public StdioTransport(JsonRpcService jsonRpc)
    {
        _jsonRpc = jsonRpc;
    }

    public async Task Run()
    {
        var stdoutOriginal = Console.Out;
        Console.SetOut(Console.Error);

        Console.WriteLine("[MCP Server] Mode STDIO démarré");

        while (true)
        {
            using var stdin = new StreamReader(Console.OpenStandardInput());
            var line = await stdin.ReadLineAsync();

            if (string.IsNullOrEmpty(line)) break;

            Console.WriteLine($"[MCP stdio] Reçu: {line[..Math.Min(100, line.Length)]}...");

            try
            {
                var request = JsonNode.Parse(line)!;
                var response = await _jsonRpc.HandleRequest(request);

                await stdoutOriginal.WriteLineAsync(response.ToJsonString());
                await stdoutOriginal.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP stdio] Erreur: {ex.Message}");
                await stdoutOriginal.FlushAsync();
            }
        }
    }
}