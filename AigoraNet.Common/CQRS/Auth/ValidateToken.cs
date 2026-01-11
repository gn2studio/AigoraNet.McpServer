using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Auth;

public record ValidateTokenQuery(string TokenKey) : IBridgeRequest<ReturnValues<ValidateTokenResult>>;

public record ValidateTokenResult(bool Success, string? MemberId = null, TokenStatus Status = TokenStatus.Expired, DateTime? ExpiresAt = null, string? Error = null);

public class ValidateTokenQueryHandler : IBridgeHandler<ValidateTokenQuery, ReturnValues<ValidateTokenResult>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<ValidateTokenQuery> _logger;

    public ValidateTokenQueryHandler(ILogger<ValidateTokenQuery> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<ValidateTokenResult>> HandleAsync(ValidateTokenQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<ValidateTokenResult>();

        var token = await _context.Tokens.FirstOrDefaultAsync(x => x.TokenKey == request.TokenKey, ct);
        if (token is null)
        {
            _logger.LogWarning("Token not found {TokenKey}", request.TokenKey);
            result.SetError("Token not found");
            result.Data = new ValidateTokenResult(false, null, TokenStatus.Expired, null, "Token not found");
            return result;
        }

        if (token.Status == TokenStatus.Revoked)
        {
            _logger.LogWarning("Token revoked {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
            result.SetError("Token revoked");
            result.Data = new ValidateTokenResult(false, token.MemberId, token.Status, token.ExpiresAt, "Token revoked");
            return result;
        }

        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value < DateTime.UtcNow)
        {
            token.Status = TokenStatus.Expired;
            await _context.SaveChangesAsync(ct);
            _logger.LogWarning("Token expired {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
            result.SetError("Token expired");
            result.Data = new ValidateTokenResult(false, token.MemberId, token.Status, token.ExpiresAt, "Token expired");
            return result;
        }

        token.LastUsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Validated token {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
        result.SetSuccess(1, new ValidateTokenResult(true, token.MemberId, token.Status, token.ExpiresAt));
        return result;
    }
}

public static class ValidateTokenHandler
{
    public static async Task<ValidateTokenResult> Handle(ValidateTokenQuery query, DefaultContext db, ILogger<ValidateTokenQuery> logger, CancellationToken ct)
    {
        var bridge = await new ValidateTokenQueryHandler(logger, db).HandleAsync(query, ct);
        return bridge.Data ?? new ValidateTokenResult(bridge.Success, null, TokenStatus.Expired, null, bridge.Message);
    }
}
