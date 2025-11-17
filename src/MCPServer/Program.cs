using MCPServer.Services;

Console.WriteLine("MCP Server démarré!");

var _ = new OllamaService("http://localhost:11434", "gemma3:4b");