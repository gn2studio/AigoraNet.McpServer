using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Boards;

public record CreateBoardMasterCommand(string MasterCode, string Title, string? OwnerId, string? Section, string? Site, string? Description, string? Icon, BoardTypecs BoardType, int Seq, string CreatedBy) : IBridgeRequest<ReturnValues<BoardMaster>>;
public record UpdateBoardMasterCommand(string Id, string? Title, string? Section, string? Site, string? Description, string? Icon, BoardTypecs? BoardType, int? Seq, string UpdatedBy) : IBridgeRequest<ReturnValues<BoardMaster>>;
public record DeleteBoardMasterCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<BoardMaster>>;
public record GetBoardMasterQuery(string Id) : IBridgeRequest<ReturnValues<BoardMaster>>;
public record ListBoardMastersQuery(string? Section = null, string? Site = null) : IBridgeRequest<ReturnValues<List<BoardMaster>>>;

public record BoardMasterResult(bool Success, string? Error = null, BoardMaster? Master = null);
public record BoardMasterListResult(bool Success, string? Error = null, IReadOnlyList<BoardMaster>? Masters = null);

public class CreateBoardMasterCommandHandler : IBridgeHandler<CreateBoardMasterCommand, ReturnValues<BoardMaster>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<CreateBoardMasterCommand> _logger;

    public CreateBoardMasterCommandHandler(ILogger<CreateBoardMasterCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardMaster>> HandleAsync(CreateBoardMasterCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardMaster>();

        if (string.IsNullOrWhiteSpace(request.MasterCode))
        {
            result.SetError("MasterCode required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            result.SetError("Title required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            result.SetError("CreatedBy required");
            return result;
        }

        var exists = await _context.BoardMasters.AsNoTracking().AnyAsync(x => x.MasterCode == request.MasterCode, ct);
        if (exists)
        {
            result.SetError("MasterCode already exists");
            return result;
        }

        var master = new BoardMaster
        {
            MasterCode = request.MasterCode.Trim(),
            Title = request.Title.Trim(),
            OwnerId = request.OwnerId,
            Section = request.Section,
            Site = request.Site,
            Description = request.Description,
            Icon = request.Icon,
            BoardType = request.BoardType,
            Seq = request.Seq,
            Condition = new AuditableEntity { CreatedBy = request.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await _context.BoardMasters.AddAsync(master, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardMaster created {MasterCode}", master.MasterCode);
        result.SetSuccess(1, master);
        return result;
    }
}

public class UpdateBoardMasterCommandHandler : IBridgeHandler<UpdateBoardMasterCommand, ReturnValues<BoardMaster>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpdateBoardMasterCommand> _logger;

    public UpdateBoardMasterCommandHandler(ILogger<UpdateBoardMasterCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardMaster>> HandleAsync(UpdateBoardMasterCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardMaster>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            result.SetError("UpdatedBy required");
            return result;
        }

        var master = await _context.BoardMasters.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (master is null)
        {
            result.SetError("BoardMaster not found");
            return result;
        }
        if (!master.Condition.IsEnabled || master.Condition.Status != ConditionStatus.Active)
        {
            result.SetError("BoardMaster inactive");
            return result;
        }

        master.Title = request.Title ?? master.Title;
        master.Section = request.Section ?? master.Section;
        master.Site = request.Site ?? master.Site;
        master.Description = request.Description ?? master.Description;
        master.Icon = request.Icon ?? master.Icon;
        if (request.BoardType.HasValue) master.BoardType = request.BoardType.Value;
        if (request.Seq.HasValue) master.Seq = request.Seq.Value;
        master.Condition.UpdatedBy = request.UpdatedBy;
        master.Condition.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardMaster updated {Id}", master.Id);
        result.SetSuccess(1, master);
        return result;
    }
}

public class DeleteBoardMasterCommandHandler : IBridgeHandler<DeleteBoardMasterCommand, ReturnValues<BoardMaster>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<DeleteBoardMasterCommand> _logger;

    public DeleteBoardMasterCommandHandler(ILogger<DeleteBoardMasterCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardMaster>> HandleAsync(DeleteBoardMasterCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardMaster>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.DeletedBy))
        {
            result.SetError("DeletedBy required");
            return result;
        }

        var master = await _context.BoardMasters.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (master is null)
        {
            result.SetError("BoardMaster not found");
            return result;
        }

        master.Condition.IsEnabled = false;
        master.Condition.Status = ConditionStatus.Disabled;
        master.Condition.DeletedBy = request.DeletedBy;
        master.Condition.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardMaster disabled {Id}", master.Id);
        result.SetSuccess(1, master);
        return result;
    }
}

public class GetBoardMasterQueryHandler : IBridgeHandler<GetBoardMasterQuery, ReturnValues<BoardMaster>>
{
    private readonly DefaultContext _context;

    public GetBoardMasterQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<BoardMaster>> HandleAsync(GetBoardMasterQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardMaster>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var master = await _context.BoardMasters.AsNoTracking().Include(x => x.Contents)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (master is null)
        {
            result.SetError("BoardMaster not found");
            return result;
        }

        result.SetSuccess(1, master);
        return result;
    }
}

public class ListBoardMastersQueryHandler : IBridgeHandler<ListBoardMastersQuery, ReturnValues<List<BoardMaster>>>
{
    private readonly DefaultContext _context;

    public ListBoardMastersQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<BoardMaster>>> HandleAsync(ListBoardMastersQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<BoardMaster>>();

        var q = _context.BoardMasters.AsNoTracking().Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.Section)) q = q.Where(x => x.Section == request.Section);
        if (!string.IsNullOrWhiteSpace(request.Site)) q = q.Where(x => x.Site == request.Site);
        var list = await q.OrderBy(x => x.Seq).ToListAsync(ct);

        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class BoardMasterHandlers
{
    public static async Task<BoardMasterResult> Handle(CreateBoardMasterCommand command, DefaultContext db, ILogger<CreateBoardMasterCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreateBoardMasterCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardMasterResult> Handle(UpdateBoardMasterCommand command, DefaultContext db, ILogger<UpdateBoardMasterCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpdateBoardMasterCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardMasterResult> Handle(DeleteBoardMasterCommand command, DefaultContext db, ILogger<DeleteBoardMasterCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeleteBoardMasterCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardMasterResult> Handle(GetBoardMasterQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new GetBoardMasterQueryHandler(db).HandleAsync(query, ct);
        return new BoardMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardMasterListResult> Handle(ListBoardMastersQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListBoardMastersQueryHandler(db).HandleAsync(query, ct);
        return new BoardMasterListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
