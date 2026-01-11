using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Tokens;

public record RevokeTokenCommand(string TokenKey, string RevokedBy) : IBridgeRequest<ReturnValues<Token>>;
public record GetTokenQuery(string TokenKey) : IBridgeRequest<ReturnValues<Token>>;
public record ListTokensByMemberQuery(string MemberId, bool IncludeExpired = false) : IBridgeRequest<ReturnValues<List<Token>>>;

public record TokenResult(bool Success, string? Error = null, Token? Token = null);
public record TokenListResult(bool Success, string? Error = null, IReadOnlyList<Token>? Tokens = null);

public class RevokeTokenCommandHandler : IBridgeHandler<RevokeTokenCommand, ReturnValues<Token>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<RevokeTokenCommand> _logger;

    public RevokeTokenCommandHandler(ILogger<RevokeTokenCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<Token>> HandleAsync(RevokeTokenCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Token>();

        if (string.IsNullOrWhiteSpace(request.TokenKey))
        {
            result.SetError("TokenKey required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.RevokedBy))
        {
            result.SetError("RevokedBy required");
            return result;
        }

        var token = await _context.Tokens.FirstOrDefaultAsync(x => x.TokenKey == request.TokenKey, ct);
        if (token is null)
        {
            result.SetError("Token not found");
            return result;
        }

        token.Status = TokenStatus.Revoked;
        token.RevokedAt = DateTime.UtcNow;
        token.Condition.UpdatedBy = request.RevokedBy;
        token.Condition.LastUpdate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Token revoked {TokenKey}", token.TokenKey);
        result.SetSuccess(1, token);
        return result;
    }
}

public class GetTokenQueryHandler : IBridgeHandler<GetTokenQuery, ReturnValues<Token>>
{
    private readonly DefaultContext _context;

    public GetTokenQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<Token>> HandleAsync(GetTokenQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<Token>();

        if (string.IsNullOrWhiteSpace(request.TokenKey))
        {
            result.SetError("TokenKey required");
            return result;
        }

        var token = await _context.Tokens.AsNoTracking().Include(t => t.Member).FirstOrDefaultAsync(x => x.TokenKey == request.TokenKey, ct);
        if (token is null)
        {
            result.SetError("Token not found");
            return result;
        }

        result.SetSuccess(1, token);
        return result;
    }
}

public class ListTokensByMemberQueryHandler : IBridgeHandler<ListTokensByMemberQuery, ReturnValues<List<Token>>>
{
    private readonly DefaultContext _context;

    public ListTokensByMemberQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<Token>>> HandleAsync(ListTokensByMemberQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<Token>>();

        if (string.IsNullOrWhiteSpace(request.MemberId))
        {
            result.SetError("MemberId required");
            return result;
        }

        var q = _context.Tokens.AsNoTracking().Where(t => t.MemberId == request.MemberId);
        if (!request.IncludeExpired) q = q.Where(t => t.Status == TokenStatus.Issued);
        var list = await q.OrderByDescending(t => t.IssuedAt).ToListAsync(ct);

        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class TokenHandlers
{
    public static async Task<TokenResult> Handle(RevokeTokenCommand command, DefaultContext db, ILogger<RevokeTokenCommand> logger, CancellationToken ct)
    {
        var bridge = await new RevokeTokenCommandHandler(logger, db).HandleAsync(command, ct);
        return new TokenResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<TokenResult> Handle(GetTokenQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new GetTokenQueryHandler(db).HandleAsync(query, ct);
        return new TokenResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<TokenListResult> Handle(ListTokensByMemberQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListTokensByMemberQueryHandler(db).HandleAsync(query, ct);
        return new TokenListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
