using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Tokens;

// Query to list all tokens for the owner of a provided token
public record ListTokensByOwnerQuery(string TokenKey);

// Query to get prompts mapped to a specific token
public record GetPromptsForTokenQuery(string TokenKey);

// Result types
public record TokenSummaryListResult(bool Success, string? Error = null, IReadOnlyList<TokenSummaryDTO>? Tokens = null);
public record PromptListResult(bool Success, string? Error = null, IReadOnlyList<PromptTemplateDTO>? Prompts = null);

public static class TokenQueryHandlers
{
    /// <summary>
    /// Lists all tokens for the owner of the provided token.
    /// Validates the token, resolves the owner, and returns all tokens for that owner.
    /// </summary>
    public static async Task<TokenSummaryListResult> Handle(
        ListTokensByOwnerQuery query,
        DefaultContext db,
        ILogger<ListTokensByOwnerQuery> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.TokenKey))
        {
            logger.LogWarning("ListTokensByOwner failed: empty token key");
            return new TokenSummaryListResult(false, "TokenKey is required");
        }

        // Find the token and resolve the owner
        var token = await db.Tokens
            .AsNoTracking()
            .Where(t => t.TokenKey == query.TokenKey)
            .Select(t => new { t.MemberId, t.Status })
            .FirstOrDefaultAsync(ct);

        if (token is null)
        {
            logger.LogWarning("ListTokensByOwner failed: token not found {TokenKey}", MaskTokenKey(query.TokenKey));
            return new TokenSummaryListResult(true, null, Array.Empty<TokenSummaryDTO>());
        }

        // Query all tokens for this owner with status/enabled filters
        var tokens = await db.Tokens
            .AsNoTracking()
            .Where(t => t.MemberId == token.MemberId)
            .Where(t => t.Condition.IsEnabled && t.Condition.Status == ConditionStatus.Active)
            .Where(t => t.Status == TokenStatus.Issued)
            .OrderByDescending(t => t.IssuedAt)
            .Select(t => new TokenSummaryDTO
            {
                Id = t.Id,
                Name = t.Name ?? string.Empty,
                Status = t.Status.ToString(),
                IssuedAt = t.IssuedAt,
                ExpiresAt = t.ExpiresAt,
                LastUsedAt = t.LastUsedAt,
                IsEnabled = t.Condition.IsEnabled,
                MaskedTokenKey = MaskTokenKey(t.TokenKey)
            })
            .ToListAsync(ct);

        logger.LogInformation("ListTokensByOwner returned {Count} tokens for member {MemberId}",
            tokens.Count, token.MemberId);

        return new TokenSummaryListResult(true, null, tokens);
    }

    /// <summary>
    /// Gets all prompts mapped to a specific token.
    /// Validates the token is active/enabled and returns associated prompts.
    /// </summary>
    public static async Task<PromptListResult> Handle(
        GetPromptsForTokenQuery query,
        DefaultContext db,
        ILogger<GetPromptsForTokenQuery> logger,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.TokenKey))
        {
            logger.LogWarning("GetPromptsForToken failed: empty token key");
            return new PromptListResult(false, "TokenKey is required");
        }

        // Validate the token exists and is active
        var token = await db.Tokens
            .AsNoTracking()
            .Where(t => t.TokenKey == query.TokenKey)
            .Where(t => t.Condition.IsEnabled && t.Condition.Status == ConditionStatus.Active)
            .Where(t => t.Status == TokenStatus.Issued)
            .FirstOrDefaultAsync(ct);

        if (token is null)
        {
            logger.LogWarning("GetPromptsForToken failed: token not found or inactive {TokenKey}",
                MaskTokenKey(query.TokenKey));
            return new PromptListResult(false, "Token not found or inactive");
        }

        // Query prompts mapped to this token
        var prompts = await db.TokenPromptMappings
            .AsNoTracking()
            .Where(m => m.TokenId == token.Id)
            .Where(m => m.Condition.IsEnabled && m.Condition.Status == ConditionStatus.Active)
            .Include(m => m.PromptTemplate)
            .Where(m => m.PromptTemplate!.Condition.IsEnabled && m.PromptTemplate.Condition.Status == ConditionStatus.Active)
            .Select(m => new PromptTemplateDTO
            {
                Id = m.PromptTemplate!.Id,
                Name = m.PromptTemplate.Name,
                Content = m.PromptTemplate.Content,
                Description = m.PromptTemplate.Description,
                Version = m.PromptTemplate.Version,
                Locale = m.PromptTemplate.Locale,
                IsEnabled = m.PromptTemplate.Condition.IsEnabled
            })
            .ToListAsync(ct);

        logger.LogInformation("GetPromptsForToken returned {Count} prompts for token {TokenKey}",
            prompts.Count, MaskTokenKey(query.TokenKey));

        return new PromptListResult(true, null, prompts);
    }

    /// <summary>
    /// Masks a token key for logging/display purposes.
    /// Shows only first 4 and last 4 characters.
    /// </summary>
    private static string MaskTokenKey(string tokenKey)
    {
        if (string.IsNullOrEmpty(tokenKey) || tokenKey.Length <= 8)
            return "****";

        return $"{tokenKey[..4]}...{tokenKey[^4..]}";
    }
}
