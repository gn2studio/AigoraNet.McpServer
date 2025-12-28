using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Logs;

public record CreateLogItemCommand(string Message, string Level, string MessageTemplate, string LogEvent, string Properties, string? Exception = null, DateTimeOffset? TimeStamp = null);
public record GetLogItemQuery(long Id);
public record ListLogItemsQuery(string? Level = null, DateTimeOffset? From = null, DateTimeOffset? To = null, int Take = 100);

public record LogItemResult(bool Success, string? Error = null, LogItem? LogItem = null);
public record LogItemListResult(bool Success, string? Error = null, IReadOnlyList<LogItem>? Items = null);

public static class LogItemHandlers
{
    public static async Task<LogItemResult> Handle(CreateLogItemCommand command, DefaultContext db, ILogger<CreateLogItemCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Message)) return new LogItemResult(false, "Message required");
        if (string.IsNullOrWhiteSpace(command.Level)) return new LogItemResult(false, "Level required");
        if (string.IsNullOrWhiteSpace(command.MessageTemplate)) return new LogItemResult(false, "MessageTemplate required");
        if (string.IsNullOrWhiteSpace(command.LogEvent)) return new LogItemResult(false, "LogEvent required");
        if (string.IsNullOrWhiteSpace(command.Properties)) return new LogItemResult(false, "Properties required");

        var item = new LogItem
        {
            Message = command.Message,
            Level = command.Level,
            MessageTemplate = command.MessageTemplate,
            LogEvent = command.LogEvent,
            Properties = command.Properties,
            Exception = command.Exception ?? string.Empty,
            TimeStamp = command.TimeStamp ?? DateTimeOffset.UtcNow
        };

        await db.Logs.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("LogItem created {Id}", item.Id);
        return new LogItemResult(true, null, item);
    }

    public static async Task<LogItemResult> Handle(GetLogItemQuery query, DefaultContext db, CancellationToken ct)
    {
        var item = await db.Logs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id, ct);
        return item is null ? new LogItemResult(false, "Log not found") : new LogItemResult(true, null, item);
    }

    public static async Task<LogItemListResult> Handle(ListLogItemsQuery query, DefaultContext db, CancellationToken ct)
    {
        var q = db.Logs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Level)) q = q.Where(x => x.Level == query.Level);
        if (query.From.HasValue) q = q.Where(x => x.TimeStamp >= query.From.Value);
        if (query.To.HasValue) q = q.Where(x => x.TimeStamp <= query.To.Value);
        var list = await q.OrderByDescending(x => x.TimeStamp).Take(query.Take).ToListAsync(ct);
        return new LogItemListResult(true, null, list);
    }
}
