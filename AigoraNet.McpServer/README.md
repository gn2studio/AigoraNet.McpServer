# MCP Server

This README was created using the C# MCP server project template.
It demonstrates how you can easily create an MCP server using C# and publish it as a NuGet package.

The MCP server is built as a self-contained application and does not require the .NET runtime to be installed on the target machine.
However, since it is self-contained, it must be built for each target platform separately.
By default, the template is configured to build for:
* `win-x64`
* `win-arm64`
* `osx-arm64`
* `linux-x64`
* `linux-arm64`
* `linux-musl-x64`

If your users require more platforms to be supported, update the list of runtime identifiers in the project's `<RuntimeIdentifiers />` element.

See [aka.ms/nuget/mcp/guide](https://aka.ms/nuget/mcp/guide) for the full guide.

Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](http://aka.ms/dotnet-mcp-template-survey).

## Checklist before publishing to NuGet.org

- Test the MCP server locally using the steps below.
- Update the package metadata in the .csproj file, in particular the `<PackageId>`.
- Update `.mcp/server.json` to declare your MCP server's inputs.
  - See [configuring inputs](https://aka.ms/nuget/mcp/guide/configuring-inputs) for more details.
- Pack the project using `dotnet pack`.

The `bin/Release` directory will contain the package file (.nupkg), which can be [published to NuGet.org](https://learn.microsoft.com/nuget/nuget-org/publish-a-package).

## Developing locally

To test this MCP server from source code (locally) without using a built MCP server package, you can configure your IDE to run the project directly using `dotnet run`.

```json
{
  "servers": {
    "AigoraNet.McpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "<PATH TO PROJECT DIRECTORY>"
      ]
    }
  }
}
```

Refer to the VS Code or Visual Studio documentation for more information on configuring and using MCP servers:

- [Use MCP servers in VS Code (Preview)](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [Use MCP servers in Visual Studio (Preview)](https://learn.microsoft.com/visualstudio/ide/mcp-servers)

## Testing the MCP Server

Once configured, the MCP server provides the following tools:

### Available Tools

1. **get_random_number** - Generates a random number between specified min and max values.
   - Example: "Give me 3 random numbers"

2. **list_tokens_for_owner** - Lists all tokens owned by the owner of a provided token.
   - Input: `tokenKey` (string) - The token key to identify the owner
   - Returns: List of tokens with metadata (ID, name, status, dates, masked token key)
   - Security: Token keys are masked to prevent exposure
   - Example: "List all tokens for the owner of token xyz"

3. **get_prompts_for_token** - Gets all prompts (prompt templates) mapped to a specific token.
   - Input: `tokenKey` (string) - The token key to retrieve prompts for
   - Returns: List of prompt templates (ID, name, content, description, version, locale)
   - Example: "Get all prompts associated with token xyz"

### Configuration

The MCP server requires a database connection string to access token and prompt data. Set the connection string using one of these methods:

1. Environment variable: `AIGORANET_CONNECTION_STRING`
2. Configuration file: Add `"ConnectionStrings:DefaultConnection"` to your appsettings.json

Example:
```bash
export AIGORANET_CONNECTION_STRING="Server=localhost;Database=AigoraNet;Trusted_Connection=True;"
dotnet run --project AigoraNet.McpServer
```

### Edge Cases

- **Non-existent token**: Returns empty list (no error)
- **Revoked/expired token**: Returns error message for prompt queries; excluded from token lists
- **Token with no prompts**: Returns empty prompt list (no error)
- **Empty token key**: Returns validation error

## Publishing to NuGet.org

1. Run `dotnet pack -c Release` to create the NuGet package
2. Publish to NuGet.org with `dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json`

## Using the MCP Server from NuGet.org

Once the MCP server package is published to NuGet.org, you can configure it in your preferred IDE. Both VS Code and Visual Studio use the `dnx` command to download and install the MCP server package from NuGet.org.

- **VS Code**: Create a `<WORKSPACE DIRECTORY>/.vscode/mcp.json` file
- **Visual Studio**: Create a `<SOLUTION DIRECTORY>\.mcp.json` file

For both VS Code and Visual Studio, the configuration file uses the following server definition:

```json
{
  "servers": {
    "AigoraNet.McpServer": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "<your package ID here>",
        "--version",
        "<your package version here>",
        "--yes"
      ]
    }
  }
}
```

## More information

.NET MCP servers use the [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) C# SDK. For more information about MCP:

- [Official Documentation](https://modelcontextprotocol.io/)
- [Protocol Specification](https://spec.modelcontextprotocol.io/)
- [GitHub Organization](https://github.com/modelcontextprotocol)
- [MCP C# SDK](https://modelcontextprotocol.github.io/csharp-sdk)
