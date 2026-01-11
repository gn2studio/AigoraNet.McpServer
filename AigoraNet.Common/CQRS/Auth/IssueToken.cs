using System.Security.Cryptography;
using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Auth;

public record IssueTokenCommand(string MemberId, string? Name, TimeSpan? Lifetime) : IBridgeRequest<ReturnValues<IssueTokenResult>>;

public record IssueTokenResult(string? TokenKey, bool Success, string? Error = null, DateTime? ExpiresAt = null);

public class IssueTokenCommandHandler : IBridgeHandler<IssueTokenCommand, ReturnValues<IssueTokenResult>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<IssueTokenCommand> _logger;

    public IssueTokenCommandHandler(ILogger<IssueTokenCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<IssueTokenResult>> HandleAsync(IssueTokenCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<IssueTokenResult>();

        var member = await _context.Members.FirstOrDefaultAsync(x => x.Id == request.MemberId, ct);
        if (member is null)
        {
            _logger.LogWarning("Cannot issue token, member not found {MemberId}", request.MemberId);
            result.SetError("Member not found");
            return result;
        }

        var expiresAt = request.Lifetime.HasValue ? DateTime.UtcNow.Add(request.Lifetime.Value) : (DateTime?)null;
        var tokenKey = GenerateTokenKey();

        var token = new Token
        {
            Id = Guid.NewGuid().ToString(),
            MemberId = request.MemberId,
            TokenKey = tokenKey,
            Name = request.Name,
            ExpiresAt = expiresAt,
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = request.MemberId }
        };

        await _context.Tokens.AddAsync(token, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Issued token {TokenKey} for member {MemberId}", tokenKey, request.MemberId);
        result.SetSuccess(1, new IssueTokenResult(tokenKey, true, null, expiresAt));
        return result;
    }

    private static string GenerateTokenKey()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public static class IssueTokenHandler
{
    public static async Task<IssueTokenResult> Handle(IssueTokenCommand command, DefaultContext db, ILogger<IssueTokenCommand> logger, CancellationToken ct)
    {
        var bridge = await new IssueTokenCommandHandler(logger, db).HandleAsync(command, ct);
        return bridge.Data ?? new IssueTokenResult(null, bridge.Success, bridge.Message);
    }
}
