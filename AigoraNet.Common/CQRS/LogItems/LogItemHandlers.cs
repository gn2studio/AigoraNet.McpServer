using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AigoraNet.Common.CQRS.Logs;

public record CreateLogItemCommand(string Message, string Level, string MessageTemplate, string LogEvent, string Properties, string? Exception = null, DateTimeOffset? TimeStamp = null) : IBridgeRequest<ReturnValues<LogItem>>;
public record GetLogItemQuery(long Id) : IBridgeRequest<ReturnValues<LogItem>>;
public record ListLogItemsQuery(string? Level = null, DateTimeOffset? From = null, DateTimeOffset? To = null, int Take = 100) : IBridgeRequest<ReturnValues<List<LogItem>>>;

public record LogItemResult(bool Success, string? Error = null, LogItem? LogItem = null);
public record LogItemListResult(bool Success, string? Error = null, IReadOnlyList<LogItem>? Items = null);

public class CreateLogItemCommandHandler : IBridgeHandler<CreateLogItemCommand, ReturnValues<LogItem>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<CreateLogItemCommand> _logger;

    public CreateLogItemCommandHandler(ILogger<CreateLogItemCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<LogItem>> HandleAsync(CreateLogItemCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<LogItem>();

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            result.SetError("Message required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Level))
        {
            result.SetError("Level required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.MessageTemplate))
        {
            result.SetError("MessageTemplate required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.LogEvent))
        {
            result.SetError("LogEvent required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Properties))
        {
            result.SetError("Properties required");
            return result;
        }

        var item = new LogItem
        {
            Message = request.Message,
            Level = request.Level,
            MessageTemplate = request.MessageTemplate,
            LogEvent = request.LogEvent,
            Properties = request.Properties,
            Exception = request.Exception ?? string.Empty,
            TimeStamp = request.TimeStamp ?? DateTimeOffset.UtcNow
        };

        await _context.Logs.AddAsync(item, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("LogItem created {Id}", item.Id);

        result.SetSuccess(1, item);
        return result;
    }
}

public class GetLogItemQueryHandler : IBridgeHandler<GetLogItemQuery, ReturnValues<LogItem>>
{
    private readonly DefaultContext _context;

    public GetLogItemQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<LogItem>> HandleAsync(GetLogItemQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<LogItem>();

        var item = await _context.Logs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (item is null)
        {
            result.SetError("Log not found");
            return result;
        }

        result.SetSuccess(1, item);
        return result;
    }
}

public class ListLogItemsQueryHandler : IBridgeHandler<ListLogItemsQuery, ReturnValues<List<LogItem>>>
{
    private readonly DefaultContext _context;

    public ListLogItemsQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<LogItem>>> HandleAsync(ListLogItemsQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<LogItem>>();

        var q = _context.Logs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Level)) q = q.Where(x => x.Level == request.Level);
        if (request.From.HasValue) q = q.Where(x => x.TimeStamp >= request.From.Value);
        if (request.To.HasValue) q = q.Where(x => x.TimeStamp <= request.To.Value);

        var list = await q.OrderByDescending(x => x.TimeStamp).Take(request.Take).ToListAsync(ct);

        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class LogItemHandlers
{
    public static async Task<LogItemResult> Handle(CreateLogItemCommand command, DefaultContext db, ILogger<CreateLogItemCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreateLogItemCommandHandler(logger, db).HandleAsync(command, ct);
        return new LogItemResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<LogItemResult> Handle(GetLogItemQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new GetLogItemQueryHandler(db).HandleAsync(query, ct);
        return new LogItemResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<LogItemListResult> Handle(ListLogItemsQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListLogItemsQueryHandler(db).HandleAsync(query, ct);
        return new LogItemListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
