using AigoraNet.Common;
using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace AigoraNet.WebApi.Middleware;

public sealed class TokenValidationMiddleware : IMiddleware
{
    private static readonly HashSet<string> _excludedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/auth",
        "/swagger",
        "/openapi",
        "/scalar"
    };

    private const string TokenHeaderName = "X-Token-Key";
    public const string HttpContextTokenKey = "X-Token-Key";
    public const string HttpContextMemberIdKey = "MemberId";

    private readonly DefaultContext _db;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    public TokenValidationMiddleware(DefaultContext db, ILogger<TokenValidationMiddleware> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.HasValue ? context.Request.Path.Value! : string.Empty;
        if (_excludedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(TokenHeaderName, out var tokenHeader) || string.IsNullOrWhiteSpace(tokenHeader))
        {
            _logger.LogWarning("Token missing on path {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token key required");
            return;
        }

        var tokenKey = tokenHeader.ToString();
        var token = await _db.Tokens.FirstOrDefaultAsync(x => x.TokenKey == tokenKey, context.RequestAborted);
        if (token is null)
        {
            _logger.LogWarning("Token not found {TokenKey} on path {Path}", tokenKey, path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid token");
            return;
        }

        if (token.Status == TokenStatus.Revoked)
        {
            _logger.LogWarning("Token revoked {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token revoked");
            return;
        }

        if (token.ExpiresAt.HasValue && token.ExpiresAt.Value < DateTime.UtcNow)
        {
            token.Status = TokenStatus.Expired;
            await _db.SaveChangesAsync(context.RequestAborted);
            _logger.LogWarning("Token expired {TokenKey} for member {MemberId}", token.TokenKey, token.MemberId);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Token expired");
            return;
        }

        token.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(context.RequestAborted);

        context.Items[HttpContextTokenKey] = token.TokenKey;
        context.Items[HttpContextMemberIdKey] = token.MemberId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "TokenKey", token.TokenKey },
            { "MemberId", token.MemberId }
        }))
        {
            _logger.LogInformation("Token validated for member {MemberId}", token.MemberId);
            await next(context);
        }
    }
}
