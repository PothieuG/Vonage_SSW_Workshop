using MCPServer.Services;
using MCPServer.Transport;

Console.WriteLine("MCP Server démarré!");

var _ = Environment.GetCommandLineArgs();

var ollama = new OllamaService("http://localhost:11434", "gemma3:4b");
var tools = new ToolsService(ollama);
var jsonRpc = new JsonRpcService(tools);

var transport = new HttpTransport(jsonRpc);
await transport.Run();
