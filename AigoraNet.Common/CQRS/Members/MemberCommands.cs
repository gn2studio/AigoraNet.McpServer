using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Common.Library.Crypto;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Members;

public record CreateMemberCommand(string Email, string PasswordHash, string? NickName, string? Photo, string? Bio) : IBridgeRequest<ReturnValues<Member>>;
public record UpdateMemberCommand(string Id, string? NickName, string? Photo, string? Bio, string UpdatedBy) : IBridgeRequest<ReturnValues<Member>>;
public record DeleteMemberCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<Member>>;
public record ConfirmMemberEmailCommand(string MemberId, string UpdatedBy) : IBridgeRequest<ReturnValues<Member>>;

public class CreateMemberCommandHandler : IBridgeHandler<CreateMemberCommand, ReturnValues<Member>>
{
    private readonly ILogger<CreateMemberCommandHandler> _logger;
    private readonly DefaultContext _context;

    public CreateMemberCommandHandler(ILogger<CreateMemberCommandHandler> logger, DefaultContext _db) : base()
    {
        _logger = logger;
        _context = _db;
    }

    public async Task<ReturnValues<Member>> HandleAsync(CreateMemberCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Member>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.SetError("Email required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            result.SetError("PasswordHash required");
            return result;
        }

        var exists = await _context.Members.AsNoTracking().AnyAsync(x => x.Email == request.Email, ct);
        if (exists)
        {
            result.SetError("Email already exists");
            return result;
        }

        var crypto = new SHA512Handler();
        var member = new Member
        {
            Id = Guid.NewGuid().ToString(),
            Email = request.Email.Trim(),
            PasswordHash = crypto.Encrypt(request.PasswordHash),
            NickName = request.NickName,
            Photo = request.Photo,
            Bio = request.Bio,
            Type = Member.MemberType.User,
            Condition = new AuditableEntity { CreatedBy = "client", RegistDate = DateTime.UtcNow, IsEnabled = true, Status = ConditionStatus.Active }
        };

        await _context.Members.AddAsync(member, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Member created {Email}", member.Email);

        result.SetSuccess(1, member);

        return result;
    }
}

public class UpdateMemberCommandHandler : IBridgeHandler<UpdateMemberCommand, ReturnValues<Member>>
{
    private readonly ILogger<UpdateMemberCommandHandler> _logger;
    private readonly DefaultContext _context;

    public UpdateMemberCommandHandler(ILogger<UpdateMemberCommandHandler> logger, DefaultContext _db) : base()
    {
        _logger = logger;
        _context = _db;
    }

    public async Task<ReturnValues<Member>> HandleAsync(UpdateMemberCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Member>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var member = await _context.Members.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (member is null)
        {
            result.SetError("Member not found");
            return result;
        }

        if (!member.Condition.IsEnabled || member.Condition.Status != ConditionStatus.Active)
        {
            result.SetError("Member inactive");
            return result;
        }

        member.NickName = request.NickName ?? member.NickName;
        member.Photo = request.Photo ?? member.Photo;
        member.Bio = request.Bio ?? member.Bio;
        member.Condition.UpdatedBy = request.UpdatedBy;
        member.Condition.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Member updated {MemberId}", member.Id);

        result.SetSuccess(1, member);

        return result;
    }
}

public class DeleteMemberCommandHandler : IBridgeHandler<DeleteMemberCommand, ReturnValues<Member>>
{
    private readonly ILogger<DeleteMemberCommandHandler> _logger;
    private readonly DefaultContext _context;

    public DeleteMemberCommandHandler(ILogger<DeleteMemberCommandHandler> logger, DefaultContext _db) : base()
    {
        _logger = logger;
        _context = _db;
    }

    public async Task<ReturnValues<Member>> HandleAsync(DeleteMemberCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Member>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var member = await _context.Members.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (member is null)
        {
            result.SetError("Member not found");
            return result;
        }

        member.Condition.IsEnabled = false;
        member.Condition.Status = ConditionStatus.Disabled;
        member.Condition.DeletedBy = request.DeletedBy;
        member.Condition.DeletedDate = DateTime.UtcNow;

        var tokens = await _context.Tokens.Where(t => t.MemberId == member.Id).ToListAsync(ct);
        foreach (var token in tokens)
        {
            token.Status = TokenStatus.Revoked;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Member disabled {MemberId}", member.Id);

        result.SetSuccess(1, member);

        return result;
    }
}

public class ConfirmMemberEmailCommandHandler : IBridgeHandler<ConfirmMemberEmailCommand, ReturnValues<Member>>
{
    private readonly ILogger<ConfirmMemberEmailCommandHandler> _logger;
    private readonly DefaultContext _context;

    public ConfirmMemberEmailCommandHandler(ILogger<ConfirmMemberEmailCommandHandler> logger, DefaultContext db) : base()
    {
        _logger = logger;
        _context = db;
    }

    public async Task<ReturnValues<Member>> HandleAsync(ConfirmMemberEmailCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Member>();
        if (string.IsNullOrWhiteSpace(request.MemberId))
        {
            result.SetError("MemberId required");
            return result;
        }

        var member = await _context.Members.FirstOrDefaultAsync(x => x.Id == request.MemberId, ct);
        if (member is null)
        {
            result.SetError("Member not found");
            return result;
        }

        member.IsEmailConfirm = true;
        member.EmailConfirmDate = DateTime.UtcNow;
        member.Condition.UpdatedBy = request.UpdatedBy;
        member.Condition.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Member email confirmed {MemberId}", member.Id);
        result.SetSuccess(1, member);
        return result;
    }
}