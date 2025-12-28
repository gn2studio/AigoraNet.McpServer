using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Members;

public record CreateMemberCommand(string Email, string PasswordHash, string? NickName, string? Photo, string? Bio, Member.MemberType Type, string CreatedBy);
public record UpdateMemberCommand(string Id, string? NickName, string? Photo, string? Bio, Member.MemberType? Type, string UpdatedBy);
public record DeleteMemberCommand(string Id, string DeletedBy);
public record GetMemberQuery(string Id);
public record ListMembersQuery(Member.MemberType? Type = null);

public record MemberResult(bool Success, string? Error = null, Member? Member = null);
public record MemberListResult(bool Success, string? Error = null, IReadOnlyList<Member>? Members = null);

public static class MemberHandlers
{
    public static async Task<MemberResult> Handle(CreateMemberCommand command, DefaultContext db, ILogger<CreateMemberCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Email)) return new MemberResult(false, "Email required");
        if (string.IsNullOrWhiteSpace(command.PasswordHash)) return new MemberResult(false, "PasswordHash required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new MemberResult(false, "CreatedBy required");

        var exists = await db.Members.AsNoTracking().AnyAsync(x => x.Email == command.Email, ct);
        if (exists) return new MemberResult(false, "Email already exists");

        var member = new Member
        {
            Email = command.Email.Trim(),
            PasswordHash = command.PasswordHash,
            NickName = command.NickName,
            Photo = command.Photo,
            Bio = command.Bio,
            Type = command.Type,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.Members.AddAsync(member, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Member created {Email}", member.Email);
        return new MemberResult(true, null, member);
    }

    public static async Task<MemberResult> Handle(UpdateMemberCommand command, DefaultContext db, ILogger<UpdateMemberCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new MemberResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new MemberResult(false, "UpdatedBy required");

        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (member is null) return new MemberResult(false, "Member not found");
        if (!member.Condition.IsEnabled || member.Condition.Status != ConditionStatus.Active) return new MemberResult(false, "Member inactive");

        member.NickName = command.NickName ?? member.NickName;
        member.Photo = command.Photo ?? member.Photo;
        member.Bio = command.Bio ?? member.Bio;
        if (command.Type.HasValue) member.Type = command.Type.Value;
        member.Condition.UpdatedBy = command.UpdatedBy;
        member.Condition.LastUpdate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Member updated {MemberId}", member.Id);
        return new MemberResult(true, null, member);
    }

    public static async Task<MemberResult> Handle(DeleteMemberCommand command, DefaultContext db, ILogger<DeleteMemberCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new MemberResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new MemberResult(false, "DeletedBy required");

        var member = await db.Members.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (member is null) return new MemberResult(false, "Member not found");

        member.Condition.IsEnabled = false;
        member.Condition.Status = ConditionStatus.Disabled;
        member.Condition.DeletedBy = command.DeletedBy;
        member.Condition.DeletedDate = DateTime.UtcNow;

        var tokens = await db.Tokens.Where(t => t.MemberId == member.Id).ToListAsync(ct);
        foreach (var token in tokens)
        {
            token.Status = TokenStatus.Revoked;
            token.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Member disabled {MemberId}", member.Id);
        return new MemberResult(true, null, member);
    }

    public static async Task<MemberResult> Handle(GetMemberQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Id)) return new MemberResult(false, "Id required");
        var member = await db.Members.AsNoTracking()
            .Where(x => x.Id == query.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active)
            .FirstOrDefaultAsync(ct);
        return member is null ? new MemberResult(false, "Member not found") : new MemberResult(true, null, member);
    }

    public static async Task<MemberListResult> Handle(ListMembersQuery query, DefaultContext db, CancellationToken ct)
    {
        var q = db.Members.AsNoTracking().Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (query.Type.HasValue) q = q.Where(x => x.Type == query.Type.Value);
        var list = await q.ToListAsync(ct);
        return new MemberListResult(true, null, list);
    }
}
