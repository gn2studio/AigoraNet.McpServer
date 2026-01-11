using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Auth;

public record RegisterUserCommand(string Email, string Password, string? NickName) : IBridgeRequest<ReturnValues<RegisterUserResult>>;

public record RegisterUserResult(string? MemberId, bool Success, string? Error = null);

public class RegisterUserCommandHandler : IBridgeHandler<RegisterUserCommand, ReturnValues<RegisterUserResult>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<RegisterUserCommand> _logger;

    public RegisterUserCommandHandler(ILogger<RegisterUserCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<RegisterUserResult>> HandleAsync(RegisterUserCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<RegisterUserResult>();

        var exists = await _context.Members.AnyAsync(x => x.Email == request.Email, ct);
        if (exists)
        {
            result.SetError("Email already exists");
            return result;
        }

        var member = new Member
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email,
            PasswordHash = request.Password,
            NickName = request.NickName,
            Condition = new AuditableEntity { CreatedBy = request.Email }
        };

        await _context.Members.AddAsync(member, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Registered user {Email}", request.Email);
        result.SetSuccess(1, new RegisterUserResult(member.Id, true));
        return result;
    }
}

public static class RegisterUserHandler
{
    public static async Task<RegisterUserResult> Handle(RegisterUserCommand command, DefaultContext db, ILogger<RegisterUserCommand> logger, CancellationToken ct)
    {
        var bridge = await new RegisterUserCommandHandler(logger, db).HandleAsync(command, ct);
        return bridge.Data ?? new RegisterUserResult(null, bridge.Success, bridge.Message);
    }
}
