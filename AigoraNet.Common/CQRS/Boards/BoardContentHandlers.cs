using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AigoraNet.Common.CQRS.Boards;

public record CreateBoardContentCommand(string MasterId, string CategoryId, string Title, string Content, string? Answer, string? OwnerId, string CreatedBy);
public record UpdateBoardContentCommand(string Id, string Title, string Content, string? Answer, string UpdatedBy, string? AnswerOwnerId = null, DateTime? AnswerDate = null);
public record DeleteBoardContentCommand(string Id, string DeletedBy);
public record GetBoardContentQuery(string Id);
public record ListBoardContentsQuery(string MasterId, string? CategoryId = null);

public record BoardContentResult(bool Success, string? Error = null, BoardContent? Content = null);
public record BoardContentListResult(bool Success, string? Error = null, IReadOnlyList<BoardContent>? Items = null);

public static class BoardContentHandlers
{
    public static async Task<BoardContentResult> Handle(CreateBoardContentCommand command, DefaultContext db, ILogger<CreateBoardContentCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.MasterId)) return new BoardContentResult(false, "MasterId required");
        if (string.IsNullOrWhiteSpace(command.CategoryId)) return new BoardContentResult(false, "CategoryId required");
        if (string.IsNullOrWhiteSpace(command.Title)) return new BoardContentResult(false, "Title required");
        if (string.IsNullOrWhiteSpace(command.Content)) return new BoardContentResult(false, "Content required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new BoardContentResult(false, "CreatedBy required");

        var master = await db.BoardMasters.FirstOrDefaultAsync(x => x.Id == command.MasterId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (master is null) return new BoardContentResult(false, "BoardMaster not found or inactive");
        var category = await db.BoardCategories.FirstOrDefaultAsync(x => x.Id == command.CategoryId, ct);
        if (category is null) return new BoardContentResult(false, "Category not found");

        var entity = new BoardContent
        {
            MasterId = command.MasterId,
            CategoryId = command.CategoryId,
            Title = command.Title.Trim(),
            Content = HttpUtility.UrlDecode(command.Content),
            Answer = string.IsNullOrWhiteSpace(command.Answer) ? null : HttpUtility.UrlDecode(command.Answer!),
            OwnerId = command.OwnerId,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.BoardContents.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardContent created {Id}", entity.Id);
        return new BoardContentResult(true, null, entity);
    }

    public static async Task<BoardContentResult> Handle(UpdateBoardContentCommand command, DefaultContext db, ILogger<UpdateBoardContentCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new BoardContentResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.Title)) return new BoardContentResult(false, "Title required");
        if (string.IsNullOrWhiteSpace(command.Content)) return new BoardContentResult(false, "Content required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new BoardContentResult(false, "UpdatedBy required");

        var entity = await db.BoardContents.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new BoardContentResult(false, "BoardContent not found");
        if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active) return new BoardContentResult(false, "BoardContent inactive");

        entity.Title = command.Title.Trim();
        entity.Content = HttpUtility.UrlDecode(command.Content);
        entity.Answer = string.IsNullOrWhiteSpace(command.Answer) ? entity.Answer : HttpUtility.UrlDecode(command.Answer!);
        entity.AnswerDate = command.AnswerDate ?? entity.AnswerDate;
        entity.AnwerOwnerId = command.AnswerOwnerId ?? entity.AnwerOwnerId;
        entity.Condition.UpdatedBy = command.UpdatedBy;
        entity.Condition.LastUpdate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardContent updated {Id}", entity.Id);
        return new BoardContentResult(true, null, entity);
    }

    public static async Task<BoardContentResult> Handle(DeleteBoardContentCommand command, DefaultContext db, ILogger<DeleteBoardContentCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new BoardContentResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new BoardContentResult(false, "DeletedBy required");
        var entity = await db.BoardContents.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new BoardContentResult(false, "BoardContent not found");

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = command.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("BoardContent disabled {Id}", entity.Id);
        return new BoardContentResult(true, null, entity);
    }

    public static async Task<BoardContentResult> Handle(GetBoardContentQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Id)) return new BoardContentResult(false, "Id required");
        var entity = await db.BoardContents.AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Master)
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        return entity is null ? new BoardContentResult(false, "BoardContent not found") : new BoardContentResult(true, null, entity);
    }

    public static async Task<BoardContentListResult> Handle(ListBoardContentsQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.MasterId)) return new BoardContentListResult(false, "MasterId required");
        var q = db.BoardContents.AsNoTracking().Where(x => x.MasterId == query.MasterId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(query.CategoryId)) q = q.Where(x => x.CategoryId == query.CategoryId);
        var list = await q.OrderByDescending(x => x.Condition.RegistDate).ToListAsync(ct);
        return new BoardContentListResult(true, null, list);
    }
}
