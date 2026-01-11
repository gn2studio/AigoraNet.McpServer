using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Comments;

public record UpsertCommentHistoryCommand(string CommentId, string OwnerId, CommentHistoryType HistoryType) : IBridgeRequest<ReturnValues<CommentHistory>>;
public record RemoveCommentHistoryCommand(string CommentId, string OwnerId) : IBridgeRequest<ReturnValues<CommentHistory>>;
public record CommentHistoryResult(bool Success, string? Error = null, CommentHistory? History = null);

public class UpsertCommentHistoryCommandHandler : IBridgeHandler<UpsertCommentHistoryCommand, ReturnValues<CommentHistory>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpsertCommentHistoryCommand> _logger;

    public UpsertCommentHistoryCommandHandler(ILogger<UpsertCommentHistoryCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<CommentHistory>> HandleAsync(UpsertCommentHistoryCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<CommentHistory>();

        if (string.IsNullOrWhiteSpace(request.CommentId))
        {
            result.SetError("CommentId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.OwnerId))
        {
            result.SetError("OwnerId required");
            return result;
        }

        var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == request.CommentId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (comment is null)
        {
            result.SetError("Comment not found or inactive");
            return result;
        }

        var history = await _context.CommentHistory.FirstOrDefaultAsync(x => x.CommentId == request.CommentId && x.OwnerId == request.OwnerId, ct);
        if (history is null)
        {
            history = new CommentHistory
            {
                CommentId = request.CommentId,
                OwnerId = request.OwnerId,
                HistoryType = request.HistoryType,
                RegistDate = DateTime.UtcNow
            };
            await _context.CommentHistory.AddAsync(history, ct);
        }
        else
        {
            history.HistoryType = request.HistoryType;
            history.RegistDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Comment history upserted {CommentId} {OwnerId}", request.CommentId, request.OwnerId);
        result.SetSuccess(1, history);
        return result;
    }
}

public class RemoveCommentHistoryCommandHandler : IBridgeHandler<RemoveCommentHistoryCommand, ReturnValues<CommentHistory>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<RemoveCommentHistoryCommand> _logger;

    public RemoveCommentHistoryCommandHandler(ILogger<RemoveCommentHistoryCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<CommentHistory>> HandleAsync(RemoveCommentHistoryCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<CommentHistory>();

        if (string.IsNullOrWhiteSpace(request.CommentId))
        {
            result.SetError("CommentId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.OwnerId))
        {
            result.SetError("OwnerId required");
            return result;
        }

        var history = await _context.CommentHistory.FirstOrDefaultAsync(x => x.CommentId == request.CommentId && x.OwnerId == request.OwnerId, ct);
        if (history is null)
        {
            result.SetError("History not found");
            return result;
        }

        _context.CommentHistory.Remove(history);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Comment history removed {CommentId} {OwnerId}", request.CommentId, request.OwnerId);
        result.SetSuccess(1, history);
        return result;
    }
}

public static class CommentHistoryHandlers
{
    public static async Task<CommentHistoryResult> Handle(UpsertCommentHistoryCommand command, DefaultContext db, ILogger<UpsertCommentHistoryCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpsertCommentHistoryCommandHandler(logger, db).HandleAsync(command, ct);
        return new CommentHistoryResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<CommentHistoryResult> Handle(RemoveCommentHistoryCommand command, DefaultContext db, ILogger<RemoveCommentHistoryCommand> logger, CancellationToken ct)
    {
        var bridge = await new RemoveCommentHistoryCommandHandler(logger, db).HandleAsync(command, ct);
        return new CommentHistoryResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
