using AigoraNet.Common.Entities;
using AigoraNet.Common.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AigoraNet.Common.CQRS.Files;

public record CreateFileMasterCommand(string FileName, long FileLength, string ContentType, byte[] FileBlob, string? Properties, string? PublicUrl, string CreatedBy);
public record ReplaceFileMasterCommand(string SourceFileId, string FileName, long FileLength, string ContentType, byte[] FileBlob, string? Properties, string? PublicUrl, string ActorId);
public record DeleteFileMasterCommand(string Id, string DeletedBy);
public record GetFileMasterQuery(string Id);

public record FileMasterResult(bool Success, string? Error = null, FileMaster? File = null);

public static class FileMasterHandlers
{
    public static async Task<FileMasterResult> Handle(CreateFileMasterCommand command, DefaultContext db, IAzureBlobFileService blob, ILogger<CreateFileMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.FileName)) return new FileMasterResult(false, "FileName required");
        if (string.IsNullOrWhiteSpace(command.ContentType)) return new FileMasterResult(false, "ContentType required");
        if (command.FileLength <= 0 || command.FileBlob is null || command.FileBlob.Length == 0) return new FileMasterResult(false, "File content required");
        if (string.IsNullOrWhiteSpace(command.CreatedBy)) return new FileMasterResult(false, "CreatedBy required");

        var upload = await blob.UploadAsync(command.FileName, command.FileBlob, command.ContentType, ct);

        var entity = new FileMaster
        {
            Id = Guid.NewGuid().ToString(),
            FileName = command.FileName,
            FileLength = upload.Length,
            ContentType = upload.ContentType,
            FileBlob = null,
            Properties = upload.BlobName,
            PublicURL = upload.Url,
            RegistDate = DateTime.UtcNow,
            Condition = new AuditableEntity { CreatedBy = command.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await db.FileMasters.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("FileMaster created {Id} with blob {Blob}", entity.Id, upload.BlobName);
        return new FileMasterResult(true, null, entity);
    }

    public static async Task<FileMasterResult> Handle(ReplaceFileMasterCommand command, DefaultContext db, IAzureBlobFileService blob, ILogger<ReplaceFileMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.SourceFileId)) return new FileMasterResult(false, "SourceFileId required");
        if (string.IsNullOrWhiteSpace(command.FileName)) return new FileMasterResult(false, "FileName required");
        if (string.IsNullOrWhiteSpace(command.ContentType)) return new FileMasterResult(false, "ContentType required");
        if (command.FileLength <= 0 || command.FileBlob is null || command.FileBlob.Length == 0) return new FileMasterResult(false, "File content required");
        if (string.IsNullOrWhiteSpace(command.ActorId)) return new FileMasterResult(false, "ActorId required");

        var source = await db.FileMasters.FirstOrDefaultAsync(x => x.Id == command.SourceFileId, ct);
        if (source is null) return new FileMasterResult(false, "Source file not found");

        // upload new file first
        var upload = await blob.UploadAsync(command.FileName, command.FileBlob, command.ContentType, ct);

        var newEntity = new FileMaster
        {
            Id = Guid.NewGuid().ToString(),
            FileName = command.FileName,
            FileLength = upload.Length,
            ContentType = upload.ContentType,
            FileBlob = null,
            Properties = upload.BlobName,
            PublicURL = upload.Url,
            RegistDate = DateTime.UtcNow,
            Condition = new AuditableEntity { CreatedBy = command.ActorId, RegistDate = DateTime.UtcNow }
        };

        await db.FileMasters.AddAsync(newEntity, ct);

        // soft-delete old record and remove blob
        source.Condition.IsEnabled = false;
        source.Condition.Status = ConditionStatus.Disabled;
        source.Condition.DeletedBy = command.ActorId;
        source.Condition.DeletedDate = DateTime.UtcNow;
        await blob.DeleteAsync(source.Properties ?? string.Empty, ct);

        await db.SaveChangesAsync(ct);
        logger.LogInformation("File replaced: old {OldId} disabled, new {NewId} blob {Blob}", source.Id, newEntity.Id, upload.BlobName);
        return new FileMasterResult(true, null, newEntity);
    }

    public static async Task<FileMasterResult> Handle(DeleteFileMasterCommand command, DefaultContext db, IAzureBlobFileService blob, ILogger<DeleteFileMasterCommand> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Id)) return new FileMasterResult(false, "Id required");
        if (string.IsNullOrWhiteSpace(command.DeletedBy)) return new FileMasterResult(false, "DeletedBy required");
        var entity = await db.FileMasters.FirstOrDefaultAsync(x => x.Id == command.Id, ct);
        if (entity is null) return new FileMasterResult(false, "File not found");

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = command.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await blob.DeleteAsync(entity.Properties ?? string.Empty, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("FileMaster disabled {Id} and blob removed", entity.Id);
        return new FileMasterResult(true, null, entity);
    }

    public static async Task<FileMasterResult> Handle(GetFileMasterQuery query, DefaultContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Id)) return new FileMasterResult(false, "Id required");
        var entity = await db.FileMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        return entity is null ? new FileMasterResult(false, "File not found") : new FileMasterResult(true, null, entity);
    }
}
