using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Tokens;

public record RevokeTokenCommand(string TokenKey, string RevokedBy);
public record GetTokenQuery(string TokenKey);
public record ListTokensByMemberQuery(string MemberId, bool IncludeExpired = false);

public record TokenResult(bool Success, string? Error = null, Token? Token = null);
public record TokenListResult(bool Success, string? Error = null, IReadOnlyList<Token>? Tokens = null);

public static class TokenHandlers
{
    public static async Task<TokenResult> Handle(RevokeTokenCommand command, DefaultContext db, ILogger<RevokeTokenCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.TokenKey)) return new TokenResult(false, "TokenKey required");
        if (string.IsNullOrWhiteSpace(command.RevokedBy)) return new TokenResult(false, "RevokedBy required");

        var token = await db.Tokens.FirstOrDefaultAsync(x => x.TokenKey == command.TokenKey, ct);
        if (token is null) return new TokenResult(false, "Token not found");

        token.Status = TokenStatus.Revoked;
        token.RevokedAt = DateTime.UtcNow;
        token.Condition.UpdatedBy = command.RevokedBy;
        token.Condition.LastUpdate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Token revoked {TokenKey}", token.TokenKey);
        return new TokenResult(true, null, token);
    }

    public static async Task<TokenResult> Handle(GetTokenQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.TokenKey)) return new TokenResult(false, "TokenKey required");
        var token = await db.Tokens.AsNoTracking().Include(t => t.Member).FirstOrDefaultAsync(x => x.TokenKey == query.TokenKey, ct);
        return token is null ? new TokenResult(false, "Token not found") : new TokenResult(true, null, token);
    }

    public static async Task<TokenListResult> Handle(ListTokensByMemberQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.MemberId)) return new TokenListResult(false, "MemberId required");
        var q = db.Tokens.AsNoTracking().Where(t => t.MemberId == query.MemberId);
        if (!query.IncludeExpired) q = q.Where(t => t.Status == TokenStatus.Issued);
        var list = await q.OrderByDescending(t => t.IssuedAt).ToListAsync(ct);
        return new TokenListResult(true, null, list);
    }
}
