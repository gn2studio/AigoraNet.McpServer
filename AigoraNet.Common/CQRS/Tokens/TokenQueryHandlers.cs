using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Tokens;

// Query to list all tokens for the owner of a provided token
public record ListTokensByOwnerQuery(string TokenKey) : IBridgeRequest<ReturnValues<List<TokenSummaryDTO>>>;

// Query to get prompts mapped to a specific token
public record GetPromptsForTokenQuery(string TokenKey) : IBridgeRequest<ReturnValues<List<PromptTemplateDTO>>>;

// Result types
public record TokenSummaryListResult(bool Success, string? Error = null, IReadOnlyList<TokenSummaryDTO>? Tokens = null);
public record PromptListResult(bool Success, string? Error = null, IReadOnlyList<PromptTemplateDTO>? Prompts = null);

public class ListTokensByOwnerQueryHandler : IBridgeHandler<ListTokensByOwnerQuery, ReturnValues<List<TokenSummaryDTO>>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<ListTokensByOwnerQuery> _logger;

    public ListTokensByOwnerQueryHandler(ILogger<ListTokensByOwnerQuery> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<List<TokenSummaryDTO>>> HandleAsync(ListTokensByOwnerQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<TokenSummaryDTO>>();

        if (string.IsNullOrWhiteSpace(request.TokenKey))
        {
            _logger.LogWarning("ListTokensByOwner failed: empty token key");
            result.SetError("TokenKey is required");
            return result;
        }

        var token = await _context.Tokens
            .AsNoTracking()
            .Where(t => t.TokenKey == request.TokenKey)
            .Select(t => new { t.MemberId, t.Status })
            .FirstOrDefaultAsync(ct);

        if (token is null)
        {
            _logger.LogWarning("ListTokensByOwner failed: token not found {TokenKey}", TokenQueryHandlers.MaskTokenKey(request.TokenKey));
            result.SetSuccess(0, new List<TokenSummaryDTO>());
            return result;
        }

        var tokens = await _context.Tokens
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
                MaskedTokenKey = TokenQueryHandlers.MaskTokenKey(t.TokenKey)
            })
            .ToListAsync(ct);

        _logger.LogInformation("ListTokensByOwner returned {Count} tokens for member {MemberId}", tokens.Count, token.MemberId);
        result.SetSuccess(tokens.Count, tokens);
        return result;
    }
}

public class GetPromptsForTokenQueryHandler : IBridgeHandler<GetPromptsForTokenQuery, ReturnValues<List<PromptTemplateDTO>>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<GetPromptsForTokenQuery> _logger;

    public GetPromptsForTokenQueryHandler(ILogger<GetPromptsForTokenQuery> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<List<PromptTemplateDTO>>> HandleAsync(GetPromptsForTokenQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<PromptTemplateDTO>>();

        if (string.IsNullOrWhiteSpace(request.TokenKey))
        {
            _logger.LogWarning("GetPromptsForToken failed: empty token key");
            result.SetError("TokenKey is required");
            return result;
        }

        var token = await _context.Tokens
            .AsNoTracking()
            .Where(t => t.TokenKey == request.TokenKey)
            .Where(t => t.Condition.IsEnabled && t.Condition.Status == ConditionStatus.Active)
            .Where(t => t.Status == TokenStatus.Issued)
            .FirstOrDefaultAsync(ct);

        if (token is null)
        {
            _logger.LogWarning("GetPromptsForToken failed: token not found or inactive {TokenKey}", TokenQueryHandlers.MaskTokenKey(request.TokenKey));
            result.SetError("Token not found or inactive");
            return result;
        }

        var prompts = await _context.TokenPromptMappings
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

        _logger.LogInformation("GetPromptsForToken returned {Count} prompts for token {TokenKey}", prompts.Count, TokenQueryHandlers.MaskTokenKey(request.TokenKey));

        result.SetSuccess(prompts.Count, prompts);
        return result;
    }
}

public static class TokenQueryHandlers
{
    public static async Task<TokenSummaryListResult> Handle(ListTokensByOwnerQuery query, DefaultContext db, ILogger<ListTokensByOwnerQuery> logger, CancellationToken ct)
    {
        var bridge = await new ListTokensByOwnerQueryHandler(logger, db).HandleAsync(query, ct);
        return new TokenSummaryListResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<PromptListResult> Handle(GetPromptsForTokenQuery query, DefaultContext db, ILogger<GetPromptsForTokenQuery> logger, CancellationToken ct)
    {
        var bridge = await new GetPromptsForTokenQueryHandler(logger, db).HandleAsync(query, ct);
        return new PromptListResult(bridge.Success, bridge.Message, bridge.Data);
    }

    internal static string MaskTokenKey(string tokenKey)
    {
        if (string.IsNullOrEmpty(tokenKey) || tokenKey.Length <= 8)
            return "****";

        return $"{tokenKey[..4]}...{tokenKey[^4..]}";
    }
}
