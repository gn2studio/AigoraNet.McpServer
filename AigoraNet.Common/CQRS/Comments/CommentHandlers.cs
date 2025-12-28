using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AigoraNet.Common.CQRS.Comments;

public record CreateCommentCommand(string Key, string Content, string OwnerId, string CreatedBy, string? ParentId = null);
public record UpdateCommentCommand(string Id, string Content, string UpdatedBy);
public record DeleteCommentCommand(string Id, string DeletedBy);
public record ListCommentsQuery(string Key);

public record CommentResult(bool Success, string? Error = null, Comment? Comment = null);
public record CommentListResult(bool Success, string? Error = null, IReadOnlyList<Comment>? Comments = null);

public static class CommentHandlers
{
    public static async Task<CommentResult> Handle(CreateCommentCommand command, DefaultContext db, ILogger<CreateCommentCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Key)) return new CommentResult(false, "Key required");
        if (string.IsNullOrWhiteSpace(command.Content)) return new CommentResult(false, "Content required");
        if (string.IsNullOrWhiteSpace(command.OwnerId)) return new CommentResult(false, "OwnerId required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new CommentResult(false, "CreatedBy required");

        Comment? parent = null;
        var depth = 0;
        if (!string.IsNullOrWhiteSpace(command.ParentId))
        {
            parent = await db.Comments.FirstOrDefaultAsync(x => x.Id == command.ParentId, ct);
            if (parent is null) return new CommentResult(false, "Parent comment not found");
            depth = parent.Depth + 1;
        }

        var entity = new Comment
        {
            Key = command.Key.Trim(),
            Content = HttpUtility.UrlDecode(command.Content),
            OwnerId = command.OwnerId,
            ParentId = command.ParentId,
            Depth = depth,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.Comments.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Comment created {Id}", entity.Id);
        return new CommentResult(true, null, entity);
    }

    public static async Task<CommentResult> Handle(UpdateCommentCommand command, DefaultContext db, ILogger<UpdateCommentCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new CommentResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.Content)) return new CommentResult(false, "Content required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new CommentResult(false, "UpdatedBy required");

        var entity = await db.Comments.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new CommentResult(false, "Comment not found");
        if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active) return new CommentResult(false, "Comment inactive");

        entity.Content = HttpUtility.UrlDecode(command.Content);
        entity.Condition.UpdatedBy = command.UpdatedBy;
        entity.Condition.LastUpdate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Comment updated {Id}", entity.Id);
        return new CommentResult(true, null, entity);
    }

    public static async Task<CommentResult> Handle(DeleteCommentCommand command, DefaultContext db, ILogger<DeleteCommentCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new CommentResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new CommentResult(false, "DeletedBy required");
        var entity = await db.Comments.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new CommentResult(false, "Comment not found");

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = command.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Comment disabled {Id}", entity.Id);
        return new CommentResult(true, null, entity);
    }

    public static async Task<CommentListResult> Handle(ListCommentsQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Key)) return new CommentListResult(false, "Key required");
        var list = await db.Comments.AsNoTracking()
            .Where(x => x.Key == query.Key && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active)
            .OrderBy(x => x.Depth).ThenBy(x => x.Condition.RegistDate)
            .ToListAsync(ct);
        return new CommentListResult(true, null, list);
    }
}
