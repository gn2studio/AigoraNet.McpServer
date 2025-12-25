var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.AigoraNet_IdentityWeb>("aigoranet-identityweb");
builder.AddProject<Projects.AigoraNet_McpServer>("aigoranet-mcpserver");

builder.AddProject<Projects.AigoraNet_WebApi>("aigoranet-webapi");

builder.Build().Run();
