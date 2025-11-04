using System.Text.Json.Nodes;

namespace MCPServer.Services;

public class JsonRpcService
{
    private readonly ToolsService _tools;

    public JsonRpcService(ToolsService tools)
    {
        _tools = tools;
    }

    public async Task<JsonObject> HandleRequest(JsonNode request)
    {
        var method = request["method"]?.ToString() ?? "";
        var id = request["id"]?.GetValue<int>() ?? 0;

        return method switch
        {
            "initialize" => CreateInitializeResponse(id),
            "tools/list" => CreateToolsListResponse(id),
            "tools/call" => await HandleToolCall(request, id),
            _ => throw new Exception("Méthode inconnue.")
        };
    }

    private JsonObject CreateInitializeResponse(int id)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = new JsonObject
            {
                ["protocolVersion"] = "2024-11-05",
                ["serverInfo"] = new JsonObject
                {
                    ["name"] = "ollama-text-processor",
                    ["version"] = "1.0.0"
                },
                ["capabilities"] = new JsonObject
                {
                    ["tools"] = new JsonObject()
                }
            }
        };
    }

    private JsonObject CreateToolsListResponse(int id)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = new JsonObject
            {
                ["tools"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "process_transcript",
                        ["description"] = "Détecte automatiquement la langue d'un transcript, le traduit en français si nécessaire, puis le résume de manière professionnelle en français à la troisième personne.",
                        ["inputSchema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["transcript"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "Le texte transcrit à traiter"
                                }
                            },
                            ["required"] = new JsonArray("transcript")
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "summarize_text",
                        ["description"] = "Résume un texte de manière intelligente en français avec un style professionnel à la troisième personne.",
                        ["inputSchema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["text"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "Le texte à résumer"
                                }
                            },
                            ["required"] = new JsonArray("text")
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "translate_text",
                        ["description"] = "Traduit un texte vers une langue cible en reformulant de manière naturelle à la troisième personne.",
                        ["inputSchema"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["text"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "Le texte à traduire"
                                },
                                ["target_language"] = new JsonObject
                                {
                                    ["type"] = "string",
                                    ["description"] = "Langue cible (ex: français, anglais, espagnol)",
                                    ["default"] = "français"
                                }
                            },
                            ["required"] = new JsonArray("text")
                        }
                    },
                }
            }
        };
    }

    private async Task<JsonObject> HandleToolCall(JsonNode request, int id)
    {
        try
        {
            var toolName = request["params"]?["name"]?.ToString() ?? "";
            var arguments = request["params"]?["arguments"];

            Console.WriteLine($"[MCP] Exécution de l'outil: {toolName}");

            var resultText = await _tools.Execute(toolName, arguments);

            return new JsonObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = new JsonObject
                {
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = resultText
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MCP] Erreur lors de l'exécution: {ex.Message}");
            throw new Exception("Erreur lors de l'exécution de l'outil.");
        }
    }
}
