using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Boards;

public record CreateBoardCategoryCommand(string BoardMasterId, string Title, string CreatedBy);
public record UpdateBoardCategoryCommand(string Id, string Title, string UpdatedBy);
public record DeleteBoardCategoryCommand(string Id, string DeletedBy, bool Force = false);
public record ListBoardCategoriesQuery(string BoardMasterId);

public record BoardCategoryResult(bool Success, string? Error = null, BoardCategory? Category = null);
public record BoardCategoryListResult(bool Success, string? Error = null, IReadOnlyList<BoardCategory>? Categories = null);

public static class BoardCategoryHandlers
{
    public static async Task<BoardCategoryResult> Handle(CreateBoardCategoryCommand command, DefaultContext db, ILogger<CreateBoardCategoryCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.BoardMasterId)) return new BoardCategoryResult(false, "BoardMasterId required");
        if (string.IsNullOrWhiteSpace(command.Title)) return new BoardCategoryResult(false, "Title required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new BoardCategoryResult(false, "CreatedBy required");

        var master = await db.BoardMasters.FirstOrDefaultAsync(x => x.Id == command.BoardMasterId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (master is null) return new BoardCategoryResult(false, "BoardMaster not found or inactive");

        var category = new BoardCategory
        {
            BoardMasterId = master.Id,
            Title = command.Title.Trim(),
        };

        await db.BoardCategories.AddAsync(category, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardCategory created {Title}", category.Title);
        return new BoardCategoryResult(true, null, category);
    }

    public static async Task<BoardCategoryResult> Handle(UpdateBoardCategoryCommand command, DefaultContext db, ILogger<UpdateBoardCategoryCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new BoardCategoryResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.Title)) return new BoardCategoryResult(false, "Title required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new BoardCategoryResult(false, "UpdatedBy required");

        var category = await db.BoardCategories.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (category is null) return new BoardCategoryResult(false, "Category not found");

        category.Title = command.Title.Trim();
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardCategory updated {Id}", category.Id);
        return new BoardCategoryResult(true, null, category);
    }

    public static async Task<BoardCategoryResult> Handle(DeleteBoardCategoryCommand command, DefaultContext db, ILogger<DeleteBoardCategoryCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new BoardCategoryResult(false, "Id required");
        var category = await db.BoardCategories.Include(c => c.Contents).FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (category is null) return new BoardCategoryResult(false, "Category not found");

        if (!command.Force && category.Contents.Any()) return new BoardCategoryResult(false, "Category has contents");

        db.BoardCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardCategory deleted {Id}", category.Id);
        return new BoardCategoryResult(true, null, category);
    }

    public static async Task<BoardCategoryListResult> Handle(ListBoardCategoriesQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.BoardMasterId)) return new BoardCategoryListResult(false, "BoardMasterId required");
        var list = await db.BoardCategories.AsNoTracking().Where(c => c.BoardMasterId == query.BoardMasterId).OrderBy(c => c.Title).ToListAsync(ct);
        return new BoardCategoryListResult(true, null, list);
    }
}
