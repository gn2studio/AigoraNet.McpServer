using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Auth;

public record RegisterUserCommand(string Email, string Password, string? NickName);

public record RegisterUserResult(string? MemberId, bool Success, string? Error = null);

public static class RegisterUserHandler
{
    public static async Task<RegisterUserResult> Handle(RegisterUserCommand command, DefaultContext db, ILogger<RegisterUserCommand> logger, CancellationToken ct)
    {
        var exists = await db.Members.AnyAsync(x => x.Email == command.Email, ct);
        if (exists)
        {
            return new RegisterUserResult(null, false, "Email already exists");
        }

        var member = new Member
        {
            Email = command.Email,
            PasswordHash = command.Password,
            NickName = command.NickName,
            Condition = new AuditableEntity { CreatedBy = command.Email }
        };

        await db.Members.AddAsync(member, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Registered user {Email}", command.Email);
        return new RegisterUserResult(member.Id, true);
    }
}
