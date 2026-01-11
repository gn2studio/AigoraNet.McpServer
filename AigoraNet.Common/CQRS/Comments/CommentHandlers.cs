using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AigoraNet.Common.CQRS.Comments;

public record CreateCommentCommand(string Key, string Content, string OwnerId, string CreatedBy, string? ParentId = null) : IBridgeRequest<ReturnValues<Comment>>;
public record UpdateCommentCommand(string Id, string Content, string UpdatedBy) : IBridgeRequest<ReturnValues<Comment>>;
public record DeleteCommentCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<Comment>>;
public record ListCommentsQuery(string Key) : IBridgeRequest<ReturnValues<List<Comment>>>;

public record CommentResult(bool Success, string? Error = null, Comment? Comment = null);
public record CommentListResult(bool Success, string? Error = null, IReadOnlyList<Comment>? Comments = null);

public class CreateCommentCommandHandler : IBridgeHandler<CreateCommentCommand, ReturnValues<Comment>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<CreateCommentCommand> _logger;

    public CreateCommentCommandHandler(ILogger<CreateCommentCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<Comment>> HandleAsync(CreateCommentCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Comment>();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            result.SetError("Key required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            result.SetError("Content required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.OwnerId))
        {
            result.SetError("OwnerId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            result.SetError("CreatedBy required");
            return result;
        }

        Comment? parent = null;
        var depth = 0;
        if (!string.IsNullOrWhiteSpace(request.ParentId))
        {
            parent = await _context.Comments.FirstOrDefaultAsync(x => x.Id == request.ParentId, ct);
            if (parent is null)
            {
                result.SetError("Parent comment not found");
                return result;
            }
            depth = parent.Depth + 1;
        }

        var entity = new Comment
        {
            Key = request.Key.Trim(),
            Content = HttpUtility.UrlDecode(request.Content),
            OwnerId = request.OwnerId,
            ParentId = request.ParentId,
            Depth = depth,
            Condition = new AuditableEntity { CreatedBy = request.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await _context.Comments.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Comment created {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class UpdateCommentCommandHandler : IBridgeHandler<UpdateCommentCommand, ReturnValues<Comment>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpdateCommentCommand> _logger;

    public UpdateCommentCommandHandler(ILogger<UpdateCommentCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<Comment>> HandleAsync(UpdateCommentCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Comment>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
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

        var entity = await _context.Comments.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            result.SetError("Comment not found");
            return result;
        }
        if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active)
        {
            result.SetError("Comment inactive");
            return result;
        }

        entity.Content = HttpUtility.UrlDecode(request.Content);
        entity.Condition.UpdatedBy = request.UpdatedBy;
        entity.Condition.LastUpdate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Comment updated {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class DeleteCommentCommandHandler : IBridgeHandler<DeleteCommentCommand, ReturnValues<Comment>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<DeleteCommentCommand> _logger;

    public DeleteCommentCommandHandler(ILogger<DeleteCommentCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<Comment>> HandleAsync(DeleteCommentCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<Comment>();

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

        var entity = await _context.Comments.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            result.SetError("Comment not found");
            return result;
        }

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = request.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Comment disabled {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class ListCommentsQueryHandler : IBridgeHandler<ListCommentsQuery, ReturnValues<List<Comment>>>
{
    private readonly DefaultContext _context;

    public ListCommentsQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<Comment>>> HandleAsync(ListCommentsQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<Comment>>();

        if (string.IsNullOrWhiteSpace(request.Key))
        {
            result.SetError("Key required");
            return result;
        }

        var list = await _context.Comments.AsNoTracking()
            .Where(x => x.Key == request.Key && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active)
            .OrderBy(x => x.Depth).ThenBy(x => x.Condition.RegistDate)
            .ToListAsync(ct);

        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class CommentHandlers
{
    public static async Task<CommentResult> Handle(CreateCommentCommand command, DefaultContext db, ILogger<CreateCommentCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreateCommentCommandHandler(logger, db).HandleAsync(command, ct);
        return new CommentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<CommentResult> Handle(UpdateCommentCommand command, DefaultContext db, ILogger<UpdateCommentCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpdateCommentCommandHandler(logger, db).HandleAsync(command, ct);
        return new CommentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<CommentResult> Handle(DeleteCommentCommand command, DefaultContext db, ILogger<DeleteCommentCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeleteCommentCommandHandler(logger, db).HandleAsync(command, ct);
        return new CommentResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<CommentListResult> Handle(ListCommentsQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListCommentsQueryHandler(db).HandleAsync(query, ct);
        return new CommentListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
