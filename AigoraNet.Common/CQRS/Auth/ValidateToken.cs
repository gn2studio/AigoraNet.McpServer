using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Auth;

public record ValidateTokenQuery(string TokenKey);

public record ValidateTokenResult(bool Success, string? MemberId = null, TokenStatus Status = TokenStatus.Expired, DateTime? ExpiresAt = null, string? Error = null);

public static class ValidateTokenHandler
{
    public static async Task<ValidateTokenResult> Handle(ValidateTokenQuery query, DefaultContext db, ILogger<ValidateTokenQuery> logger, CancellationToken ct)
    {
        var token = await db.Tokens.FirstOrDefaultAsync(x => x.TokenKey == query.TokenKey, ct);
        if (token is null)
        {
            logger.LogWarning("Token not found {TokenKey}", query.TokenKey);
            return new ValidateTokenResult(false, null, TokenStatus.Expired, null, "Token not found");
        }

        if (token.Status == TokenStatus.Revoked)
        {
            logger.LogWarning("Token revoked {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
            return new ValidateTokenResult(false, token.MemberId, token.Status, token.ExpiresAt, "Token revoked");
        }

        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value < DateTime.UtcNow)
        {
            token.Status = TokenStatus.Expired;
            await db.SaveChangesAsync(ct);
            logger.LogWarning("Token expired {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
            return new ValidateTokenResult(false, token.MemberId, token.Status, token.ExpiresAt, "Token expired");
        }

        token.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Validated token {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
        return new ValidateTokenResult(true, token.MemberId, token.Status, token.ExpiresAt);
    }
}
