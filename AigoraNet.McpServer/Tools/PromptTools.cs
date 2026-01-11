using System.ComponentModel;
using AigoraNet.Common.CQRS.Auth;
using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.Common.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using AigoraNet.Common;

namespace AigoraNet.McpServer.Tools;

/// <summary>
/// MCP tool for keyword-based prompt retrieval with token validation.
/// </summary>
internal class PromptTools(ILogger<PromptTools> logger, IServiceProvider serviceProvider)
{
    [McpServerTool]
    [Description("Matches a requirement against stored keyword prompts and returns the prompt content (token required).")]
    public async Task<object> MatchPrompt(
        [Description("Requirement or user need text to match against keywords.")] string requirement,
        [Description("Locale filter (optional)."), DefaultValue(null)] string? locale = null,
        [Description("Allow regex keywords (default true)."), DefaultValue(true)] bool allowRegex = true,
        [Description("Token key for authentication.")] string tokenKey = "",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tokenKey))
            {
                return new { success = false, error = "Token key is required", prompt = (PromptMatchResult?)null };
            }

            await using var scope = serviceProvider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DefaultContext>();
            var validationLogger = scope.ServiceProvider.GetRequiredService<ILogger<ValidateTokenQuery>>();
            var promptLogger = scope.ServiceProvider.GetRequiredService<ILogger<GetPromptByKeywordQuery>>();
            var cache = scope.ServiceProvider.GetRequiredService<IPromptCache>();

            // Validate token
            var validation = await ValidateTokenHandler.Handle(new ValidateTokenQuery(tokenKey), db, validationLogger, cancellationToken);
            if (!validation.Success)
            {
                return new { success = false, error = validation.Error ?? "Invalid token", prompt = (PromptMatchResult?)null };
            }

            // Match prompt
            var query = new GetPromptByKeywordQuery(requirement, locale, allowRegex);
            var match = await GetPromptByKeywordHandler.Handle(query, db, cache, promptLogger, cancellationToken);

            if (!match.Success)
            {
                return new { success = false, error = match.Error ?? "Prompt not found", prompt = (PromptMatchResult?)null };
            }

            return new
            {
                success = true,
                error = (string?)null,
                prompt = new
                {
                    id = match.PromptTemplateId,
                    name = match.PromptName,
                    keyword = match.Keyword,
                    content = match.Content
                }
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error matching prompt via MCP tool");
            return new { success = false, error = "An error occurred while matching prompt", prompt = (PromptMatchResult?)null };
        }
    }
}
