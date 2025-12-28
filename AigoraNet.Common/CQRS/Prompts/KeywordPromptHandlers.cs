using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Prompts;

public record UpsertKeywordPromptCommand(string? Id, string Keyword, string PromptTemplateId, string? Locale, bool IsRegex, string? Description, string ActorId);
public record DeleteKeywordPromptCommand(string Id, string DeletedBy);
public record ListKeywordPromptsQuery(string? Locale = null, string? PromptTemplateId = null);

public record KeywordPromptResult(bool Success, string? Error = null, KeywordPrompt? KeywordPrompt = null);
public record KeywordPromptListResult(bool Success, string? Error = null, IReadOnlyList<KeywordPrompt>? Items = null);

public static class KeywordPromptHandlers
{
    public static async Task<KeywordPromptResult> Handle(UpsertKeywordPromptCommand command, DefaultContext db, ILogger<UpsertKeywordPromptCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Keyword)) return new KeywordPromptResult(false, "Keyword required");
        if (string.IsNullOrWhiteSpace(command.PromptTemplateId)) return new KeywordPromptResult(false, "PromptTemplateId required");
        if (string.IsNullOrWhiteSpace(command.ActorId)) return new KeywordPromptResult(false, "ActorId required");

        var template = await db.PromptTemplates.FirstOrDefaultAsync(x => x.Id == command.PromptTemplateId && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (template is null) return new KeywordPromptResult(false, "Prompt template not found or inactive");

        var locale = string.IsNullOrWhiteSpace(command.Locale) ? null : command.Locale;
        var keyword = command.Keyword.Trim();

        if (command.Id is null)
        {
            var exists = await db.KeywordPrompts.AsNoTracking().AnyAsync(x => x.Keyword == keyword && x.Locale == locale, ct);
            if (exists) return new KeywordPromptResult(false, "Keyword already exists for locale");

            var entity = new KeywordPrompt
            {
                Keyword = keyword,
                Locale = locale,
                PromptTemplateId = command.PromptTemplateId,
                IsRegex = command.IsRegex,
                Description = command.Description,
                Condition = new AuditableEntity { CreatedBy = command.ActorId, RegistDate = DateTime.UtcNow }
            };
            await db.KeywordPrompts.AddAsync(entity, ct);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("KeywordPrompt created {Keyword}", keyword);
            return new KeywordPromptResult(true, null, entity);
        }
        else
        {
            var entity = await db.KeywordPrompts.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
            if (entity is null) return new KeywordPromptResult(false, "KeywordPrompt not found");
            if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active) return new KeywordPromptResult(false, "KeywordPrompt inactive");

            var conflict = await db.KeywordPrompts.AsNoTracking().AnyAsync(x => x.Id != entity.Id && x.Keyword == keyword && x.Locale == locale, ct);
            if (conflict) return new KeywordPromptResult(false, "Keyword already exists for locale");

            entity.Keyword = keyword;
            entity.Locale = locale;
            entity.IsRegex = command.IsRegex;
            entity.Description = command.Description;
            entity.PromptTemplateId = command.PromptTemplateId;
            entity.Condition.UpdatedBy = command.ActorId;
            entity.Condition.LastUpdate = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            logger.LogInformation("KeywordPrompt updated {KeywordPromptId}", entity.Id);
            return new KeywordPromptResult(true, null, entity);
        }
    }

    public static async Task<KeywordPromptResult> Handle(DeleteKeywordPromptCommand command, DefaultContext db, ILogger<DeleteKeywordPromptCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new KeywordPromptResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new KeywordPromptResult(false, "DeletedBy required");
        var entity = await db.KeywordPrompts.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new KeywordPromptResult(false, "KeywordPrompt not found");
        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = command.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("KeywordPrompt disabled {Id}", entity.Id);
        return new KeywordPromptResult(true, null, entity);
    }

    public static async Task<KeywordPromptListResult> Handle(ListKeywordPromptsQuery query, DefaultContext db, CancellationToken ct)
    {
        var q = db.KeywordPrompts.AsNoTracking().Include(x => x.PromptTemplate)
            .Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(query.Locale)) q = q.Where(x => x.Locale == query.Locale);
        if (!string.IsNullOrWhiteSpace(query.PromptTemplateId)) q = q.Where(x => x.PromptTemplateId == query.PromptTemplateId);
        var list = await q.ToListAsync(ct);
        return new KeywordPromptListResult(true, null, list);
    }
}
