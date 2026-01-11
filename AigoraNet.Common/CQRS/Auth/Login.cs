using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Common.Library.Crypto;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace AigoraNet.Common.CQRS.Auth;

public record LoginCommand(string Email, string Password) : IBridgeRequest<ReturnValues<LoginResult>>;

public record LoginResult(string TokenKey, DateTime? ExpiresAt, Member Member);

public class LoginCommandHandler : IBridgeHandler<LoginCommand, ReturnValues<LoginResult>>
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(24);

    private readonly DefaultContext _context;
    private readonly ILogger<LoginCommand> _logger;

    public LoginCommandHandler(ILogger<LoginCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<LoginResult>> HandleAsync(LoginCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<LoginResult>();

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            result.SetError("Email and password are required");
            return result;
        }

        var member = await _context.Members.AsNoTracking()
            .Where(x => x.Email == request.Email)
            .Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (member is null)
        {
            _logger.LogWarning("Login failed: member not found {Email}", request.Email);
            result.SetError("Invalid credentials");
            return result;
        }

        if (!member.IsEmailConfirm)
        {
            _logger.LogWarning("Login failed: email not confirmed {Email}", request.Email);
            result.SetError("Email not confirmed. Please check your inbox and confirm your email.");
            return result;
        }

        var crypto = new SHA512Handler();
        
        if (!crypto.ValidateCheck(member.PasswordHash, request.Password))
        {
            _logger.LogWarning("Login failed: invalid password for {Email}", request.Email);
            result.SetError("Invalid credentials");
            return result;
        }

        var expiresAt = DateTime.UtcNow.Add(DefaultLifetime);
        var tokenKey = GenerateTokenKey();

        try
        {
            var token = new Token
            {
                Id = Guid.NewGuid().ToString(),
                MemberId = member.Id,
                TokenKey = tokenKey,
                Name = "login",
                ExpiresAt = expiresAt,
                Status = TokenStatus.Issued,
                Condition = new AuditableEntity { CreatedBy = member.Id }
            };

            await _context.Tokens.AddAsync(token, ct);
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed: unable to issue token for {Email}", request.Email);
            result.SetError("Login unavailable. Please try again later.");
            return result;
        }

        _logger.LogInformation("Login success: issued token for {Email}", request.Email);
        result.SetSuccess(1, new LoginResult(tokenKey, expiresAt, member));
        return result;
    }

    private static string GenerateTokenKey()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}

public static class LoginHandler
{
    public static async Task<LoginResult?> Handle(LoginCommand command, DefaultContext db, ILogger<LoginCommand> logger, CancellationToken ct)
    {
        var bridge = await new LoginCommandHandler(logger, db).HandleAsync(command, ct);
        return bridge.Data;
    }
}
