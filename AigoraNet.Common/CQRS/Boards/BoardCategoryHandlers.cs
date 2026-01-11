using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Boards;

public record CreateBoardCategoryCommand(string BoardMasterId, string Title, string CreatedBy) : IBridgeRequest<ReturnValues<BoardCategory>>;
public record UpdateBoardCategoryCommand(string Id, string Title, string UpdatedBy) : IBridgeRequest<ReturnValues<BoardCategory>>;
public record DeleteBoardCategoryCommand(string Id, string DeletedBy, bool Force = false) : IBridgeRequest<ReturnValues<BoardCategory>>;
public record ListBoardCategoriesQuery(string BoardMasterId) : IBridgeRequest<ReturnValues<List<BoardCategory>>>;

public record BoardCategoryResult(bool Success, string? Error = null, BoardCategory? Category = null);
public record BoardCategoryListResult(bool Success, string? Error = null, IReadOnlyList<BoardCategory>? Categories = null);

public class CreateBoardCategoryCommandHandler : IBridgeHandler<CreateBoardCategoryCommand, ReturnValues<BoardCategory>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<CreateBoardCategoryCommand> _logger;

    public CreateBoardCategoryCommandHandler(ILogger<CreateBoardCategoryCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardCategory>> HandleAsync(CreateBoardCategoryCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardCategory>();

        if (string.IsNullOrWhiteSpace(request.BoardMasterId))
        {
            result.SetError("BoardMasterId required");
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

        var master = await _context.BoardMasters.FirstOrDefaultAsync(x => x.Id == request.BoardMasterId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (master is null)
        {
            result.SetError("BoardMaster not found or inactive");
            return result;
        }

        var category = new BoardCategory
        {
            Id = Guid.NewGuid().ToString(),
            BoardMasterId = master.Id,
            Title = request.Title.Trim(),
        };

        await _context.BoardCategories.AddAsync(category, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardCategory created {Title}", category.Title);
        result.SetSuccess(1, category);
        return result;
    }
}

public class UpdateBoardCategoryCommandHandler : IBridgeHandler<UpdateBoardCategoryCommand, ReturnValues<BoardCategory>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpdateBoardCategoryCommand> _logger;

    public UpdateBoardCategoryCommandHandler(ILogger<UpdateBoardCategoryCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardCategory>> HandleAsync(UpdateBoardCategoryCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardCategory>();

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
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            result.SetError("UpdatedBy required");
            return result;
        }

        var category = await _context.BoardCategories.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (category is null)
        {
            result.SetError("Category not found");
            return result;
        }

        category.Title = request.Title.Trim();
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardCategory updated {Id}", category.Id);
        result.SetSuccess(1, category);
        return result;
    }
}

public class DeleteBoardCategoryCommandHandler : IBridgeHandler<DeleteBoardCategoryCommand, ReturnValues<BoardCategory>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<DeleteBoardCategoryCommand> _logger;

    public DeleteBoardCategoryCommandHandler(ILogger<DeleteBoardCategoryCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<BoardCategory>> HandleAsync(DeleteBoardCategoryCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<BoardCategory>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var category = await _context.BoardCategories.Include(c => c.Contents).FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (category is null)
        {
            result.SetError("Category not found");
            return result;
        }

        if (!request.Force && category.Contents.Any())
        {
            result.SetError("Category has contents");
            return result;
        }

        _context.BoardCategories.Remove(category);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("BoardCategory deleted {Id}", category.Id);
        result.SetSuccess(1, category);
        return result;
    }
}

public class ListBoardCategoriesQueryHandler : IBridgeHandler<ListBoardCategoriesQuery, ReturnValues<List<BoardCategory>>>
{
    private readonly DefaultContext _context;

    public ListBoardCategoriesQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<BoardCategory>>> HandleAsync(ListBoardCategoriesQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<BoardCategory>>();

        if (string.IsNullOrWhiteSpace(request.BoardMasterId))
        {
            result.SetError("BoardMasterId required");
            return result;
        }

        var list = await _context.BoardCategories.AsNoTracking().Where(c => c.BoardMasterId == request.BoardMasterId).OrderBy(c => c.Title).ToListAsync(ct);
        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class BoardCategoryHandlers
{
    public static async Task<BoardCategoryResult> Handle(CreateBoardCategoryCommand command, DefaultContext db, ILogger<CreateBoardCategoryCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreateBoardCategoryCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardCategoryResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardCategoryResult> Handle(UpdateBoardCategoryCommand command, DefaultContext db, ILogger<UpdateBoardCategoryCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpdateBoardCategoryCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardCategoryResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardCategoryResult> Handle(DeleteBoardCategoryCommand command, DefaultContext db, ILogger<DeleteBoardCategoryCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeleteBoardCategoryCommandHandler(logger, db).HandleAsync(command, ct);
        return new BoardCategoryResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<BoardCategoryListResult> Handle(ListBoardCategoriesQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListBoardCategoriesQueryHandler(db).HandleAsync(query, ct);
        return new BoardCategoryListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
