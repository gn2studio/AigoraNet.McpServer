using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AigoraNet.Common.CQRS.Boards;

public record CreateBoardContentCommand(string MasterId, string CategoryId, string Title, string Content, string? Answer, string? OwnerId, string CreatedBy) : IBridgeRequest<ReturnValues<BoardContent>>;
public record UpdateBoardContentCommand(string Id, string Title, string Content, string? Answer, string UpdatedBy, string? AnswerOwnerId = null, DateTime? AnswerDate = null) : IBridgeRequest<ReturnValues<BoardContent>>;
public record DeleteBoardContentCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<BoardContent>>;
public record GetBoardContentQuery(string Id) : IBridgeRequest<ReturnValues<BoardContent>>;
public record ListBoardContentsQuery(string MasterId, string? CategoryId = null) : IBridgeRequest<ReturnValues<List<BoardContent>>>;

public record BoardContentResult(bool Success, string? Error = null, BoardContent? Content = null);
public record BoardContentListResult(bool Success, string? Error = null, IReadOnlyList<BoardContent>? Items = null);

public class CreateBoardContentCommandHandler : IBridgeHandler<CreateBoardContentCommand, ReturnValues<BoardContent>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<CreateBoardContentCommand> _logger;

    public CreateBoardContentCommandHandler(ILogger<CreateBoardContentCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardContent>> HandleAsync(CreateBoardContentCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardContent>();

        if (string.IsNullOrWhiteSpace(request.MasterId))
        {
            result.SetError("MasterId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.CategoryId))
        {
            result.SetError("CategoryId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            result.SetError("Title required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            result.SetError("Content required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            result.SetError("CreatedBy required");
            return result;
        }

        var master = await _context.BoardMasters.FirstOrDefaultAsync(x => x.Id == request.MasterId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (master is null)
        {
            result.SetError("BoardMaster not found or inactive");
            return result;
        }
        var category = await _context.BoardCategories.FirstOrDefaultAsync(x => x.Id == request.CategoryId, ct);
        if (category is null)
        {
            result.SetError("Category not found");
            return result;
        }

        var entity = new BoardContent
        {
            MasterId = request.MasterId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Content = HttpUtility.UrlDecode(request.Content),
            Answer = string.IsNullOrWhiteSpace(request.Answer) ? null : HttpUtility.UrlDecode(request.Answer!),
            OwnerId = request.OwnerId,
            Condition = new AuditableEntity { CreatedBy = request.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await _context.BoardContents.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardContent created {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class UpdateBoardContentCommandHandler : IBridgeHandler<UpdateBoardContentCommand, ReturnValues<BoardContent>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpdateBoardContentCommand> _logger;

    public UpdateBoardContentCommandHandler(ILogger<UpdateBoardContentCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardContent>> HandleAsync(UpdateBoardContentCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardContent>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            result.SetError("Title required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            result.SetError("Content required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            result.SetError("UpdatedBy required");
            return result;
        }

        var entity = await _context.BoardContents.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            result.SetError("BoardContent not found");
            return result;
        }
        if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active)
        {
            result.SetError("BoardContent inactive");
            return result;
        }

        entity.Title = request.Title.Trim();
        entity.Content = HttpUtility.UrlDecode(request.Content);
        entity.Answer = string.IsNullOrWhiteSpace(request.Answer) ? entity.Answer : HttpUtility.UrlDecode(request.Answer!);
        entity.AnswerDate = request.AnswerDate ?? entity.AnswerDate;
        entity.AnwerOwnerId = request.AnswerOwnerId ?? entity.AnwerOwnerId;
        entity.Condition.UpdatedBy = request.UpdatedBy;
        entity.Condition.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardContent updated {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class DeleteBoardContentCommandHandler : IBridgeHandler<DeleteBoardContentCommand, ReturnValues<BoardContent>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<DeleteBoardContentCommand> _logger;

    public DeleteBoardContentCommandHandler(ILogger<DeleteBoardContentCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardContent>> HandleAsync(DeleteBoardContentCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardContent>();

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

        var entity = await _context.BoardContents.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            result.SetError("BoardContent not found");
            return result;
        }

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = request.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardContent disabled {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class GetBoardContentQueryHandler : IBridgeHandler<GetBoardContentQuery, ReturnValues<BoardContent>>
{
    private readonly DefaultContext _context;

    public GetBoardContentQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<BoardContent>> HandleAsync(GetBoardContentQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardContent>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var entity = await _context.BoardContents.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Master)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);

        if (entity is null)
        {
            result.SetError("BoardContent not found");
            return result;
        }

        result.SetSuccess(1, entity);
        return result;
    }
}

public class ListBoardContentsQueryHandler : IBridgeHandler<ListBoardContentsQuery, ReturnValues<List<BoardContent>>>
{
    private readonly DefaultContext _context;

    public ListBoardContentsQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<BoardContent>>> HandleAsync(ListBoardContentsQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<BoardContent>>();

        if (string.IsNullOrWhiteSpace(request.MasterId))
        {
            result.SetError("MasterId required");
            return result;
        }

        var q = _context.BoardContents.AsNoTracking().Where(x => x.MasterId == request.MasterId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.CategoryId)) q = q.Where(x => x.CategoryId == request.CategoryId);
        var list = await q.OrderByDescending(x => x.Condition.RegistDate).ToListAsync(ct);

        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class BoardContentHandlers
{
    public static async Task<BoardContentResult> Handle(CreateBoardContentCommand command, DefaultContext db, ILogger<CreateBoardContentCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreateBoardContentCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardContentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardContentResult> Handle(UpdateBoardContentCommand command, DefaultContext db, ILogger<UpdateBoardContentCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpdateBoardContentCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardContentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardContentResult> Handle(DeleteBoardContentCommand command, DefaultContext db, ILogger<DeleteBoardContentCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeleteBoardContentCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardContentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardContentResult> Handle(GetBoardContentQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new GetBoardContentQueryHandler(db).HandleAsync(query, ct);
        return new BoardContentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardContentListResult> Handle(ListBoardContentsQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListBoardContentsQueryHandler(db).HandleAsync(query, ct);
        return new BoardContentListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
