using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Comments;

public record UpsertCommentHistoryCommand(string CommentId, string OwnerId, CommentHistoryType HistoryType);
public record RemoveCommentHistoryCommand(string CommentId, string OwnerId);
public record CommentHistoryResult(bool Success, string? Error = null, CommentHistory? History = null);

public static class CommentHistoryHandlers
{
    public static async Task<CommentHistoryResult> Handle(UpsertCommentHistoryCommand command, DefaultContext db, ILogger<UpsertCommentHistoryCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.CommentId)) return new CommentHistoryResult(false, "CommentId required");
        if (string.IsNullOrWhiteSpace(command.OwnerId)) return new CommentHistoryResult(false, "OwnerId required");

        var comment = await db.Comments.FirstOrDefaultAsync(x => x.Id == command.CommentId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (comment is null) return new CommentHistoryResult(false, "Comment not found or inactive");

        var history = await db.CommentHistory.FirstOrDefaultAsync(x => x.CommentId == command.CommentId && x.OwnerId == command.OwnerId, ct);
        if (history is null)
        {
            history = new CommentHistory
            {
                CommentId = command.CommentId,
                OwnerId = command.OwnerId,
                HistoryType = command.HistoryType,
                RegistDate = DateTime.UtcNow
            };
            await db.CommentHistory.AddAsync(history, ct);
        }
        else
        {
            history.HistoryType = command.HistoryType;
            history.RegistDate = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Comment history upserted {CommentId} {OwnerId}", command.CommentId, command.OwnerId);
        return new CommentHistoryResult(true, null, history);
    }

    public static async Task<CommentHistoryResult> Handle(RemoveCommentHistoryCommand command, DefaultContext db, ILogger<RemoveCommentHistoryCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.CommentId)) return new CommentHistoryResult(false, "CommentId required");
        if (string.IsNullOrWhiteSpace(command.OwnerId)) return new CommentHistoryResult(false, "OwnerId required");

        var history = await db.CommentHistory.FirstOrDefaultAsync(x => x.CommentId == command.CommentId && x.OwnerId == command.OwnerId, ct);
        if (history is null) return new CommentHistoryResult(false, "History not found");

        db.CommentHistory.Remove(history);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Comment history removed {CommentId} {OwnerId}", command.CommentId, command.OwnerId);
        return new CommentHistoryResult(true, null, history);
    }
}
