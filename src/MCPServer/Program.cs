using MCPServer.Services;
using MCPServer.Transport;

Console.WriteLine("MCP Server démarré!");

var commandLineArgs = Environment.GetCommandLineArgs();

var ollama = new OllamaService("http://localhost:11434", "gemma3:4b");
var tools = new ToolsService(ollama);
var jsonRpc = new JsonRpcService(tools);

if (commandLineArgs.Contains("--stdio"))
{
    var transport = new StdioTransport(jsonRpc);
    await transport.Run();
}
else
{
    var transport = new HttpTransport(jsonRpc);
    await transport.Run();
}