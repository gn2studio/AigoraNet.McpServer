using AigoraNet.Common.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Files;

public record CreateFileMasterCommand(string FileName, long FileLength, string ContentType, byte[]? FileBlob, string? Properties, string? PublicUrl, string CreatedBy);
public record UpdateFileMasterCommand(string Id, string? FileName, long? FileLength, string? ContentType, byte[]? FileBlob, string? Properties, string? PublicUrl, string UpdatedBy);
public record DeleteFileMasterCommand(string Id, string DeletedBy);
public record GetFileMasterQuery(string Id);

public record FileMasterResult(bool Success, string? Error = null, FileMaster? File = null);

public static class FileMasterHandlers
{
    public static async Task<FileMasterResult> Handle(CreateFileMasterCommand command, DefaultContext db, ILogger<CreateFileMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.FileName)) return new FileMasterResult(false, "FileName required");
        if (string.IsNullOrWhiteSpace(command.ContentType)) return new FileMasterResult(false, "ContentType required");
        if (command.FileLength < 0) return new FileMasterResult(false, "FileLength invalid");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new FileMasterResult(false, "CreatedBy required");

        var entity = new FileMaster
        {
            FileName = command.FileName,
            FileLength = command.FileLength,
            ContentType = command.ContentType,
            FileBlob = command.FileBlob,
            Properties = command.Properties,
            PublicURL = command.PublicUrl,
            RegistDate = DateTime.UtcNow,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.FileMasters.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("FileMaster created {Id}", entity.Id);
        return new FileMasterResult(true, null, entity);
    }

    public static async Task<FileMasterResult> Handle(UpdateFileMasterCommand command, DefaultContext db, ILogger<UpdateFileMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new FileMasterResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.UpdatedBy)) return new FileMasterResult(false, "UpdatedBy required");

        var entity = await db.FileMasters.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new FileMasterResult(false, "File not found");
        if (!entity.Condition.IsEnabled || entity.Condition.Status != ConditionStatus.Active) return new FileMasterResult(false, "File inactive");

        if (!string.IsNullOrWhiteSpace(command.FileName)) entity.FileName = command.FileName;
        if (command.FileLength.HasValue) entity.FileLength = command.FileLength.Value;
        if (!string.IsNullOrWhiteSpace(command.ContentType)) entity.ContentType = command.ContentType;
        if (command.FileBlob is not null) entity.FileBlob = command.FileBlob;
        if (command.Properties is not null) entity.Properties = command.Properties;
        if (command.PublicUrl is not null) entity.PublicURL = command.PublicUrl;
        entity.Condition.UpdatedBy = command.UpdatedBy;
        entity.Condition.LastUpdate = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("FileMaster updated {Id}", entity.Id);
        return new FileMasterResult(true, null, entity);
    }

    public static async Task<FileMasterResult> Handle(DeleteFileMasterCommand command, DefaultContext db, ILogger<DeleteFileMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new FileMasterResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new FileMasterResult(false, "DeletedBy required");
        var entity = await db.FileMasters.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new FileMasterResult(false, "File not found");

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = command.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("FileMaster disabled {Id}", entity.Id);
        return new FileMasterResult(true, null, entity);
    }

    public static async Task<FileMasterResult> Handle(GetFileMasterQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Id)) return new FileMasterResult(false, "Id required");
        var entity = await db.FileMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        return entity is null ? new FileMasterResult(false, "File not found") : new FileMasterResult(true, null, entity);
    }
}
