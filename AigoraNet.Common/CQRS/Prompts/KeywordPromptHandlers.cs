using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Prompts;

public record UpsertKeywordPromptCommand(string? Id, string Keyword, string PromptTemplateId, string? Locale, bool IsRegex, string? Description, string ActorId) : IBridgeRequest<ReturnValues<KeywordPrompt>>;
public record DeleteKeywordPromptCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<KeywordPrompt>>;
public record ListKeywordPromptsQuery(string? Locale = null, string? PromptTemplateId = null) : IBridgeRequest<ReturnValues<List<KeywordPrompt>>>;

public record KeywordPromptResult(bool Success, string? Error = null, KeywordPrompt? KeywordPrompt = null);
public record KeywordPromptListResult(bool Success, string? Error = null, IReadOnlyList<KeywordPrompt>? Items = null);

public class UpsertKeywordPromptCommandHandler : IBridgeHandler<UpsertKeywordPromptCommand, ReturnValues<KeywordPrompt>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpsertKeywordPromptCommand> _logger;

    public UpsertKeywordPromptCommandHandler(ILogger<UpsertKeywordPromptCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<KeywordPrompt>> HandleAsync(UpsertKeywordPromptCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<KeywordPrompt>();

        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            result.SetError("Keyword required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.PromptTemplateId))
        {
            result.SetError("PromptTemplateId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.ActorId))
        {
            result.SetError("ActorId required");
            return result;
        }

        var template = await _context.PromptTemplates.FirstOrDefaultAsync(x => x.Id == request.PromptTemplateId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (template is null)
        {
            result.SetError("Prompt template not found or inactive");
            return result;
        }

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? null : request.Locale;
        var keyword = request.Keyword.Trim();

        if (request.Id is null)
        {
            var exists = await _context.KeywordPrompts.AsNoTracking().AnyAsync(x => x.Keyword == keyword && x.Locale == locale, ct);
            if (exists)
            {
                result.SetError("Keyword already exists for locale");
                return result;
            }

            var entity = new KeywordPrompt
            {
                Keyword = keyword,
                Locale = locale,
                PromptTemplateId = request.PromptTemplateId,
                IsRegex = request.IsRegex,
                Description = request.Description,
                Condition = new AuditableEntity { CreatedBy = request.ActorId, RegistDate = DateTime.UtcNow }
            };
            await _context.KeywordPrompts.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("KeywordPrompt created {Keyword}", keyword);
            result.SetSuccess(1, entity);
            return result;
        }
        else
        {
            var entity = await _context.KeywordPrompts.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
            if (entity is null)
            {
                result.SetError("KeywordPrompt not found");
                return result;
            }
            if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active)
            {
                result.SetError("KeywordPrompt inactive");
                return result;
            }

            var conflict = await _context.KeywordPrompts.AsNoTracking().AnyAsync(x => x.Id != entity.Id && x.Keyword == keyword && x.Locale == locale, ct);
            if (conflict)
            {
                result.SetError("Keyword already exists for locale");
                return result;
            }

            entity.Keyword = keyword;
            entity.Locale = locale;
            entity.IsRegex = request.IsRegex;
            entity.Description = request.Description;
            entity.PromptTemplateId = request.PromptTemplateId;
            entity.Condition.UpdatedBy = request.ActorId;
            entity.Condition.LastUpdate = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("KeywordPrompt updated {KeywordPromptId}", entity.Id);
            result.SetSuccess(1, entity);
            return result;
        }
    }
}

public class DeleteKeywordPromptCommandHandler : IBridgeHandler<DeleteKeywordPromptCommand, ReturnValues<KeywordPrompt>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<DeleteKeywordPromptCommand> _logger;

    public DeleteKeywordPromptCommandHandler(ILogger<DeleteKeywordPromptCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<KeywordPrompt>> HandleAsync(DeleteKeywordPromptCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<KeywordPrompt>();

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

        var entity = await _context.KeywordPrompts.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            result.SetError("KeywordPrompt not found");
            return result;
        }

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = request.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("KeywordPrompt disabled {Id}", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class ListKeywordPromptsQueryHandler : IBridgeHandler<ListKeywordPromptsQuery, ReturnValues<List<KeywordPrompt>>>
{
    private readonly DefaultContext _context;

    public ListKeywordPromptsQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<KeywordPrompt>>> HandleAsync(ListKeywordPromptsQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<KeywordPrompt>>();

        var q = _context.KeywordPrompts.AsNoTracking().Include(x => x.PromptTemplate)
            .Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.Locale)) q = q.Where(x => x.Locale == request.Locale);
        if (!string.IsNullOrWhiteSpace(request.PromptTemplateId)) q = q.Where(x => x.PromptTemplateId == request.PromptTemplateId);
        var list = await q.ToListAsync(ct);
        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class KeywordPromptHandlers
{
    public static async Task<KeywordPromptResult> Handle(UpsertKeywordPromptCommand command, DefaultContext db, ILogger<UpsertKeywordPromptCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpsertKeywordPromptCommandHandler(logger, db).HandleAsync(command, ct);
        return new KeywordPromptResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<KeywordPromptResult> Handle(DeleteKeywordPromptCommand command, DefaultContext db, ILogger<DeleteKeywordPromptCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeleteKeywordPromptCommandHandler(logger, db).HandleAsync(command, ct);
        return new KeywordPromptResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<KeywordPromptListResult> Handle(ListKeywordPromptsQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListKeywordPromptsQueryHandler(db).HandleAsync(query, ct);
        return new KeywordPromptListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
