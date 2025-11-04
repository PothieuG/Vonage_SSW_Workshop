using MCPServer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json.Nodes;

namespace MCPServer.Transport;

public class HttpTransport
{
    private readonly JsonRpcService _jsonRpc;

    public HttpTransport(JsonRpcService jsonRpc)
    {
        _jsonRpc = jsonRpc;
    }

    public async Task Run()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapPost("/mcp", async (HttpRequest request) =>
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine($"[MCP HTTP] Reçu: {body[..Math.Min(200, body.Length)]}...");

            try
            {
                var jsonRequest = JsonNode.Parse(body)!;
                var response = await _jsonRpc.HandleRequest(jsonRequest);

                Console.WriteLine($"[MCP HTTP] Réponse envoyée pour: {jsonRequest["method"]}");

                return Results.Json(response.AsObject());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP HTTP] Erreur lors du traitement de la requête MCP: {ex.Message}");
                throw new Exception("Erreur lors du traitement de la requête MCP.");
            }
        });

        await app.RunAsync("http://localhost:5000");
    }
}
