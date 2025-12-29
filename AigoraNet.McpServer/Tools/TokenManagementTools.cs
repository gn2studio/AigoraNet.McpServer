using System.ComponentModel;
using AigoraNet.Common;
using AigoraNet.Common.CQRS.Tokens;
using AigoraNet.Common.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace AigoraNet.McpServer.Tools;

/// <summary>
/// MCP tools for token management and prompt discovery.
/// These tools allow MCP clients to list tokens and retrieve associated prompts.
/// </summary>
internal class TokenManagementTools(
    ILogger<TokenManagementTools> logger,
    IServiceProvider serviceProvider)
{
    /// <summary>
    /// Lists all tokens owned by the owner of the provided token.
    /// Returns a list of tokens with masked keys for security.
    /// </summary>
    [McpServerTool]
    [Description("Lists all tokens for the owner of the provided token. Returns token metadata with masked keys for security.")]
    public async Task<object> ListTokensForOwner(
        [Description("The token key to identify the owner")] string tokenKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            var queryLogger = scope.ServiceProvider.GetRequiredService<ILogger<ListTokensByOwnerQuery>>();

            var query = new ListTokensByOwnerQuery(tokenKey);
            var result = await TokenQueryHandlers.Handle(query, db, queryLogger, cancellationToken);

            if (!result.Success)
            {
                return new
                {
                    success = false,
                    error = result.Error,
                    tokens = Array.Empty<TokenSummaryDTO>()
                };
            }

            return new
            {
                success = true,
                error = (string?)null,
                tokens = result.Tokens ?? Array.Empty<TokenSummaryDTO>()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing tokens for owner");
            return new
            {
                success = false,
                error = "An error occurred while listing tokens",
                tokens = Array.Empty<TokenSummaryDTO>()
            };
        }
    }

    /// <summary>
    /// Gets all prompts mapped to a specific token.
    /// Returns prompt templates associated with the token.
    /// </summary>
    [McpServerTool]
    [Description("Gets all prompts (prompt templates) that are mapped to a specific token. Returns prompt content, name, and metadata.")]
    public async Task<object> GetPromptsForToken(
        [Description("The token key to retrieve prompts for")] string tokenKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a scope to resolve scoped services
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            var queryLogger = scope.ServiceProvider.GetRequiredService<ILogger<GetPromptsForTokenQuery>>();

            var query = new GetPromptsForTokenQuery(tokenKey);
            var result = await TokenQueryHandlers.Handle(query, db, queryLogger, cancellationToken);

            if (!result.Success)
            {
                return new
                {
                    success = false,
                    error = result.Error,
                    prompts = Array.Empty<PromptTemplateDTO>()
                };
            }

            return new
            {
                success = true,
                error = (string?)null,
                prompts = result.Prompts ?? Array.Empty<PromptTemplateDTO>()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting prompts for token");
            return new
            {
                success = false,
                error = "An error occurred while retrieving prompts",
                prompts = Array.Empty<PromptTemplateDTO>()
            };
        }
    }
}
