using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Web;

namespace AigoraNet.Common.CQRS.Prompts;

public record CreatePromptTemplateCommand(string Name, string Content, string? Description, string? Locale, int? Version, string CreatedBy) : IBridgeRequest<ReturnValues<PromptTemplate>>;
public record UpdatePromptTemplateCommand(string Id, string? Name, string? Content, string? Description, string? Locale, int? Version, string UpdatedBy) : IBridgeRequest<ReturnValues<PromptTemplate>>;
public record DeletePromptTemplateCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<PromptTemplate>>;
public record GetPromptTemplateQuery(string Id) : IBridgeRequest<ReturnValues<PromptTemplate>>;
public record ListPromptTemplatesQuery(string? Locale = null, string? Name = null) : IBridgeRequest<ReturnValues<List<PromptTemplate>>>;

public record PromptTemplateResult(bool Success, string? Error = null, PromptTemplate? Template = null);
public record PromptTemplateListResult(bool Success, string? Error = null, IReadOnlyList<PromptTemplate>? Templates = null);

public class CreatePromptTemplateCommandHandler : IBridgeHandler<CreatePromptTemplateCommand, ReturnValues<PromptTemplate>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<CreatePromptTemplateCommand> _logger;

    public CreatePromptTemplateCommandHandler(ILogger<CreatePromptTemplateCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<PromptTemplate>> HandleAsync(CreatePromptTemplateCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<PromptTemplate>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.SetError("Name required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            result.SetError("Content required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            result.SetError("CreatedBy required");
            return result;
        }

        var name = request.Name.Trim();
        var version = request.Version ?? 1;
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? null : request.Locale;

        var exists = await _context.PromptTemplates.AsNoTracking()
            .AnyAsync(x => x.Name == name && x.Version == version && x.Locale == locale, ct);
        if (exists)
        {
            result.SetError("Template already exists for name/version/locale");
            return result;
        }

        var template = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Content = HttpUtility.UrlDecode(request.Content),
            Description = request.Description,
            Locale = locale,
            Version = version,
            Condition = new AuditableEntity { CreatedBy = request.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await _context.PromptTemplates.AddAsync(template, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("PromptTemplate created {Name} v{Version}", template.Name, template.Version);
        result.SetSuccess(1, template);
        return result;
    }
}

public class UpdatePromptTemplateCommandHandler : IBridgeHandler<UpdatePromptTemplateCommand, ReturnValues<PromptTemplate>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<UpdatePromptTemplateCommand> _logger;

    public UpdatePromptTemplateCommandHandler(ILogger<UpdatePromptTemplateCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<PromptTemplate>> HandleAsync(UpdatePromptTemplateCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<PromptTemplate>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.UpdatedBy))
        {
            result.SetError("UpdatedBy required");
            return result;
        }

        var template = await _context.PromptTemplates.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (template is null)
        {
            result.SetError("Template not found");
            return result;
        }
        if (!template.Condition.IsEnabled || template.Condition.Status != ConditionStatus.Active)
        {
            result.SetError("Template inactive");
            return result;
        }

        var newName = request.Name?.Trim() ?? template.Name;
        var newLocale = string.IsNullOrWhiteSpace(request.Locale) ? template.Locale : request.Locale;
        var newVersion = request.Version ?? template.Version;

        var conflict = await _context.PromptTemplates.AsNoTracking()
            .AnyAsync(x => x.Id != template.Id && x.Name == newName && x.Version == newVersion && x.Locale == newLocale, ct);
        if (conflict)
        {
            result.SetError("Another template with same name/version/locale exists");
            return result;
        }

        template.Name = newName;
        if (!string.IsNullOrWhiteSpace(request.Content)) template.Content = HttpUtility.UrlDecode(request.Content);
        template.Description = request.Description ?? template.Description;
        template.Locale = newLocale;
        template.Version = newVersion;
        template.Condition.UpdatedBy = request.UpdatedBy;
        template.Condition.LastUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("PromptTemplate updated {Id}", template.Id);
        result.SetSuccess(1, template);
        return result;
    }
}

public class DeletePromptTemplateCommandHandler : IBridgeHandler<DeletePromptTemplateCommand, ReturnValues<PromptTemplate>>
{
    private readonly DefaultContext _context;
    private readonly ILogger<DeletePromptTemplateCommand> _logger;

    public DeletePromptTemplateCommandHandler(ILogger<DeletePromptTemplateCommand> logger, DefaultContext db) : base()
    {
        _context = db;
        _logger = logger;
    }

    public async Task<ReturnValues<PromptTemplate>> HandleAsync(DeletePromptTemplateCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<PromptTemplate>();

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

        var template = await _context.PromptTemplates.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (template is null)
        {
            result.SetError("Template not found");
            return result;
        }

        template.Condition.IsEnabled = false;
        template.Condition.Status = ConditionStatus.Disabled;
        template.Condition.DeletedBy = request.DeletedBy;
        template.Condition.DeletedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("PromptTemplate disabled {Id}", template.Id);
        result.SetSuccess(1, template);
        return result;
    }
}

public class GetPromptTemplateQueryHandler : IBridgeHandler<GetPromptTemplateQuery, ReturnValues<PromptTemplate>>
{
    private readonly DefaultContext _context;

    public GetPromptTemplateQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<PromptTemplate>> HandleAsync(GetPromptTemplateQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<PromptTemplate>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var template = await _context.PromptTemplates.AsNoTracking()
            .Include(t => t.KeywordPrompts)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);

        if (template is null)
        {
            result.SetError("Template not found");
            return result;
        }

        result.SetSuccess(1, template);
        return result;
    }
}

public class ListPromptTemplatesQueryHandler : IBridgeHandler<ListPromptTemplatesQuery, ReturnValues<List<PromptTemplate>>>
{
    private readonly DefaultContext _context;

    public ListPromptTemplatesQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<List<PromptTemplate>>> HandleAsync(ListPromptTemplatesQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<List<PromptTemplate>>();

        var q = _context.PromptTemplates.AsNoTracking().Where(x => x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active);
        if (!string.IsNullOrWhiteSpace(request.Locale)) q = q.Where(x => x.Locale == request.Locale);
        if (!string.IsNullOrWhiteSpace(request.Name)) q = q.Where(x => x.Name.Contains(request.Name));
        var list = await q.OrderByDescending(x => x.Version).ThenBy(x => x.Name).ToListAsync(ct);

        result.SetSuccess(list.Count, list);
        return result;
    }
}

public static class PromptTemplateHandlers
{
    public static async Task<PromptTemplateResult> Handle(CreatePromptTemplateCommand command, DefaultContext db, ILogger<CreatePromptTemplateCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreatePromptTemplateCommandHandler(logger, db).HandleAsync(command, ct);
        return new PromptTemplateResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<PromptTemplateResult> Handle(UpdatePromptTemplateCommand command, DefaultContext db, ILogger<UpdatePromptTemplateCommand> logger, CancellationToken ct)
    {
        var bridge = await new UpdatePromptTemplateCommandHandler(logger, db).HandleAsync(command, ct);
        return new PromptTemplateResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<PromptTemplateResult> Handle(DeletePromptTemplateCommand command, DefaultContext db, ILogger<DeletePromptTemplateCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeletePromptTemplateCommandHandler(logger, db).HandleAsync(command, ct);
        return new PromptTemplateResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<PromptTemplateResult> Handle(GetPromptTemplateQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new GetPromptTemplateQueryHandler(db).HandleAsync(query, ct);
        return new PromptTemplateResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<PromptTemplateListResult> Handle(ListPromptTemplatesQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new ListPromptTemplatesQueryHandler(db).HandleAsync(query, ct);
        return new PromptTemplateListResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
