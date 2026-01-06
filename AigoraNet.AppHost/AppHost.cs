var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AigoraNet_McpServer>("aigoranet-mcpserver")
    .WithHttpEndpoint(name: "http", port: 5180);

builder.AddProject<Projects.AigoraNet_WebApi>("aigoranet-webapi")
    .WithHttpEndpoint(name: "http-alt", port: 4100);

builder.Build().Run();
