using System.Security.Cryptography;
using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Auth;

public record IssueTokenCommand(string MemberId, string? Name, TimeSpan? Lifetime);

public record IssueTokenResult(string? TokenKey, bool Success, string? Error = null, DateTime? ExpiresAt = null);

public static class IssueTokenHandler
{
    public static async Task<IssueTokenResult> Handle(IssueTokenCommand command, DefaultContext db, ILogger<IssueTokenCommand> logger, CancellationToken ct)
    {
        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == command.MemberId, ct);
        if (member is null)
        {
            logger.LogWarning("Cannot issue token, member not found {MemberId}", command.MemberId);
            return new IssueTokenResult(null, false, "Member not found");
        }

        var expiresAt = command.Lifetime.HasValue ? DateTime.UtcNow.Add(command.Lifetime.Value) : (DateTime?)null;
        var tokenKey = GenerateTokenKey();

        var token = new Token
        {
            MemberId = command.MemberId,
            TokenKey = tokenKey,
            Name = command.Name,
            ExpiresAt = expiresAt,
            Status = TokenStatus.Issued,
            Condition = new AuditableEntity { CreatedBy = command.MemberId }
        };

        await db.Tokens.AddAsync(token, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Issued token {TokenKey} for member {MemberId}", tokenKey, command.MemberId);
        return new IssueTokenResult(tokenKey, true, null, expiresAt);
    }

    private static string GenerateTokenKey()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
