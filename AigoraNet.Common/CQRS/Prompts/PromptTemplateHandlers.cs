using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AigoraNet.Common.CQRS.Prompts;

public record CreatePromptTemplateCommand(string Name, string Content, string? Description, string? Locale, int? Version, string CreatedBy);
public record UpdatePromptTemplateCommand(string Id, string? Name, string? Content, string? Description, string? Locale, int? Version, string UpdatedBy);
public record DeletePromptTemplateCommand(string Id, string DeletedBy);
public record GetPromptTemplateQuery(string Id);
public record ListPromptTemplatesQuery(string? Locale = null, string? Name = null);

public record PromptTemplateResult(bool Success, string? Error = null, PromptTemplate? Template = null);
public record PromptTemplateListResult(bool Success, string? Error = null, IReadOnlyList<PromptTemplate>? Templates = null);

public static class PromptTemplateHandlers
{
    public static async Task<PromptTemplateResult> Handle(CreatePromptTemplateCommand command, DefaultContext db, ILogger<CreatePromptTemplateCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name)) return new PromptTemplateResult(false, "Name required");
        if (string.IsNullOrWhiteSpace(command.Content)) return new PromptTemplateResult(false, "Content required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new PromptTemplateResult(false, "CreatedBy required");

        var name = command.Name.Trim();
        var version = command.Version ?? 1;
        var locale = string.IsNullOrWhiteSpace(command.Locale) ? null : command.Locale;

        var exists = await db.PromptTemplates.AsNoTracking()
            .AnyAsync(x => x.Name == name && x.Version == version && x.Locale == locale, ct);
        if (exists) return new PromptTemplateResult(false, "Template already exists for name/version/locale");

        var template = new PromptTemplate
        {
            Name = name,
            Content = HttpUtility.UrlDecode(command.Content),
            Description = command.Description,
            Locale = locale,
            Version = version,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.PromptTemplates.AddAsync(template, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("PromptTemplate created {Name} v{Version}", template.Name, template.Version);
        return new PromptTemplateResult(true, null, template);
    }

    public static async Task<PromptTemplateResult> Handle(UpdatePromptTemplateCommand command, DefaultContext db, ILogger<UpdatePromptTemplateCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new PromptTemplateResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new PromptTemplateResult(false, "UpdatedBy required");

        var template = await db.PromptTemplates.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (template is null) return new PromptTemplateResult(false, "Template not found");
        if (!template.Condition.IsEnabled || template.Condition.Status != ConditionStatus.Active) return new PromptTemplateResult(false, "Template inactive");

        var newName = command.Name?.Trim() ?? template.Name;
        var newLocale = string.IsNullOrWhiteSpace(command.Locale) ? template.Locale : command.Locale;
        var newVersion = command.Version ?? template.Version;

        var conflict = await db.PromptTemplates.AsNoTracking()
            .AnyAsync(x => x.Id != template.Id && x.Name == newName && x.Version == newVersion && x.Locale == newLocale, ct);
        if (conflict) return new PromptTemplateResult(false, "Another template with same name/version/locale exists");

        template.Name = newName;
        if (!string.IsNullOrWhiteSpace(command.Content)) template.Content = HttpUtility.UrlDecode(command.Content);
        template.Description = command.Description ?? template.Description;
        template.Locale = newLocale;
        template.Version = newVersion;
        template.Condition.UpdatedBy = command.UpdatedBy;
        template.Condition.LastUpdate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("PromptTemplate updated {Id}", template.Id);
        return new PromptTemplateResult(true, null, template);
    }

    public static async Task<PromptTemplateResult> Handle(DeletePromptTemplateCommand command, DefaultContext db, ILogger<DeletePromptTemplateCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new PromptTemplateResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new PromptTemplateResult(false, "DeletedBy required");

        var template = await db.PromptTemplates.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (template is null) return new PromptTemplateResult(false, "Template not found");

        template.Condition.IsEnabled = false;
        template.Condition.Status = ConditionStatus.Disabled;
        template.Condition.DeletedBy = command.DeletedBy;
        template.Condition.DeletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("PromptTemplate disabled {Id}", template.Id);
        return new PromptTemplateResult(true, null, template);
    }

    public static async Task<PromptTemplateResult> Handle(GetPromptTemplateQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Id)) return new PromptTemplateResult(false, "Id required");
        var template = await db.PromptTemplates.AsNoTracking()
            .Include(t => t.KeywordPrompts)
            .FirstOrDefaultAsync(x => x.Id == query.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        return template is null ? new PromptTemplateResult(false, "Template not found") : new PromptTemplateResult(true, null, template);
    }

    public static async Task<PromptTemplateListResult> Handle(ListPromptTemplatesQuery query, DefaultContext db, CancellationToken ct)
    {
        var q = db.PromptTemplates.AsNoTracking().Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(query.Locale)) q = q.Where(x => x.Locale == query.Locale);
        if (!string.IsNullOrWhiteSpace(query.Name)) q = q.Where(x => x.Name.Contains(query.Name));
        var list = await q.OrderByDescending(x => x.Version).ThenBy(x => x.Name).ToListAsync(ct);
        return new PromptTemplateListResult(true, null, list);
    }
}
