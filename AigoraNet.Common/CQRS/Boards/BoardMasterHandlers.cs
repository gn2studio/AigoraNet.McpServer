using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Boards;

public record CreateBoardMasterCommand(string MasterCode, string Title, string? OwnerId, string? Section, string? Site, string? Description, string? Icon, BoardTypecs BoardType, int Seq, string CreatedBy);
public record UpdateBoardMasterCommand(string Id, string? Title, string? Section, string? Site, string? Description, string? Icon, BoardTypecs? BoardType, int? Seq, string UpdatedBy);
public record DeleteBoardMasterCommand(string Id, string DeletedBy);
public record GetBoardMasterQuery(string Id);
public record ListBoardMastersQuery(string? Section = null, string? Site = null);

public record BoardMasterResult(bool Success, string? Error = null, BoardMaster? Master = null);
public record BoardMasterListResult(bool Success, string? Error = null, IReadOnlyList<BoardMaster>? Masters = null);

public static class BoardMasterHandlers
{
    public static async Task<BoardMasterResult> Handle(CreateBoardMasterCommand command, DefaultContext db, ILogger<CreateBoardMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.MasterCode)) return new BoardMasterResult(false, "MasterCode required");
        if (string.IsNullOrWhiteSpace(command.Title)) return new BoardMasterResult(false, "Title required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new BoardMasterResult(false, "CreatedBy required");

        var exists = await db.BoardMasters.AsNoTracking().AnyAsync(x => x.MasterCode == command.MasterCode, ct);
        if (exists) return new BoardMasterResult(false, "MasterCode already exists");

        var master = new BoardMaster
        {
            MasterCode = command.MasterCode.Trim(),
            Title = command.Title.Trim(),
            OwnerId = command.OwnerId,
            Section = command.Section,
            Site = command.Site,
            Description = command.Description,
            Icon = command.Icon,
            BoardType = command.BoardType,
            Seq = command.Seq,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.BoardMasters.AddAsync(master, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardMaster created {MasterCode}", master.MasterCode);
        return new BoardMasterResult(true, null, master);
    }

    public static async Task<BoardMasterResult> Handle(UpdateBoardMasterCommand command, DefaultContext db, ILogger<UpdateBoardMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new BoardMasterResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new BoardMasterResult(false, "UpdatedBy required");

        var master = await db.BoardMasters.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (master is null) return new BoardMasterResult(false, "BoardMaster not found");
        if (!master.Condition.IsEnabled || master.Condition.Status != ConditionStatus.Active) return new BoardMasterResult(false, "BoardMaster inactive");

        master.Title = command.Title ?? master.Title;
        master.Section = command.Section ?? master.Section;
        master.Site = command.Site ?? master.Site;
        master.Description = command.Description ?? master.Description;
        master.Icon = command.Icon ?? master.Icon;
        if (command.BoardType.HasValue) master.BoardType = command.BoardType.Value;
        if (command.Seq.HasValue) master.Seq = command.Seq.Value;
        master.Condition.UpdatedBy = command.UpdatedBy;
        master.Condition.LastUpdate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardMaster updated {Id}", master.Id);
        return new BoardMasterResult(true, null, master);
    }

    public static async Task<BoardMasterResult> Handle(DeleteBoardMasterCommand command, DefaultContext db, ILogger<DeleteBoardMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new BoardMasterResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new BoardMasterResult(false, "DeletedBy required");

        var master = await db.BoardMasters.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (master is null) return new BoardMasterResult(false, "BoardMaster not found");

        master.Condition.IsEnabled = false;
        master.Condition.Status = ConditionStatus.Disabled;
        master.Condition.DeletedBy = command.DeletedBy;
        master.Condition.DeletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardMaster disabled {Id}", master.Id);
        return new BoardMasterResult(true, null, master);
    }

    public static async Task<BoardMasterResult> Handle(GetBoardMasterQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Id)) return new BoardMasterResult(false, "Id required");
        var master = await db.BoardMasters.AsNoTracking().Include(x => x.Contents)
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        return master is null ? new BoardMasterResult(false, "BoardMaster not found") : new BoardMasterResult(true, null, master);
    }

    public static async Task<BoardMasterListResult> Handle(ListBoardMastersQuery query, DefaultContext db, CancellationToken ct)
    {
        var q = db.BoardMasters.AsNoTracking().Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(query.Section)) q = q.Where(x => x.Section == query.Section);
        if (!string.IsNullOrWhiteSpace(query.Site)) q = q.Where(x => x.Site == query.Site);
        var list = await q.OrderBy(x => x.Seq).ToListAsync(ct);
        return new BoardMasterListResult(true, null, list);
    }
}
