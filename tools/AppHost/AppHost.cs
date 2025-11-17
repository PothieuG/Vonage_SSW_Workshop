using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddProject<WebApi>("api")
    .WithExternalHttpEndpoints();

builder
    .AddProject<MCPServer>("mcpserver");

builder.Build().Run();