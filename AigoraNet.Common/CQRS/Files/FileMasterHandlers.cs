using AigoraNet.Common.Abstracts;
using AigoraNet.Common.Entities;
using GN2.Common.Library.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GN2.Core;

namespace AigoraNet.Common.CQRS.Files;

public record CreateFileMasterCommand(string FileName, long FileLength, string ContentType, byte[] FileBlob, string? Properties, string? PublicUrl, string CreatedBy) : IBridgeRequest<ReturnValues<FileMaster>>;
public record ReplaceFileMasterCommand(string SourceFileId, string FileName, long FileLength, string ContentType, byte[] FileBlob, string? Properties, string? PublicUrl, string ActorId) : IBridgeRequest<ReturnValues<FileMaster>>;
public record DeleteFileMasterCommand(string Id, string DeletedBy) : IBridgeRequest<ReturnValues<FileMaster>>;
public record GetFileMasterQuery(string Id) : IBridgeRequest<ReturnValues<FileMaster>>;

public record FileMasterResult(bool Success, string? Error = null, FileMaster? File = null);

public class CreateFileMasterCommandHandler : IBridgeHandler<CreateFileMasterCommand, ReturnValues<FileMaster>>
{
    private readonly DefaultContext _context;
    private readonly IAzureBlobFileService _blob;
    private readonly ILogger<CreateFileMasterCommand> _logger;

    public CreateFileMasterCommandHandler(ILogger<CreateFileMasterCommand> logger, DefaultContext db, IAzureBlobFileService blob) : base()
    {
        _context = db;
        _blob = blob;
        _logger = logger;
    }

    public async Task<ReturnValues<FileMaster>> HandleAsync(CreateFileMasterCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<FileMaster>();

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            result.SetError("FileName required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            result.SetError("ContentType required");
            return result;
        }
        if (request.FileLength <= 0 || request.FileBlob is null || request.FileBlob.Length == 0)
        {
            result.SetError("File content required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            result.SetError("CreatedBy required");
            return result;
        }

        var upload = await _blob.UploadAsync(request.FileName, request.FileBlob, request.ContentType, ct);

        var entity = new FileMaster
        {
            Id = Guid.NewGuid().ToString(),
            FileName = request.FileName,
            FileLength = upload.Length,
            ContentType = upload.ContentType,
            FileBlob = null,
            Properties = upload.BlobName,
            PublicURL = upload.Url,
            RegistDate = DateTime.UtcNow,
            Condition = new AuditableEntity { CreatedBy = request.CreatedBy, RegistDate = DateTime.UtcNow }
        };

        await _context.FileMasters.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("FileMaster created {Id} with blob {Blob}", entity.Id, upload.BlobName);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class ReplaceFileMasterCommandHandler : IBridgeHandler<ReplaceFileMasterCommand, ReturnValues<FileMaster>>
{
    private readonly DefaultContext _context;
    private readonly IAzureBlobFileService _blob;
    private readonly ILogger<ReplaceFileMasterCommand> _logger;

    public ReplaceFileMasterCommandHandler(ILogger<ReplaceFileMasterCommand> logger, DefaultContext db, IAzureBlobFileService blob) : base()
    {
        _context = db;
        _blob = blob;
        _logger = logger;
    }

    public async Task<ReturnValues<FileMaster>> HandleAsync(ReplaceFileMasterCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<FileMaster>();

        if (string.IsNullOrWhiteSpace(request.SourceFileId))
        {
            result.SetError("SourceFileId required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            result.SetError("FileName required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            result.SetError("ContentType required");
            return result;
        }
        if (request.FileLength <= 0 || request.FileBlob is null || request.FileBlob.Length == 0)
        {
            result.SetError("File content required");
            return result;
        }
        if (string.IsNullOrWhiteSpace(request.ActorId))
        {
            result.SetError("ActorId required");
            return result;
        }

        var source = await _context.FileMasters.FirstOrDefaultAsync(x => x.Id == request.SourceFileId, ct);
        if (source is null)
        {
            result.SetError("Source file not found");
            return result;
        }

        var upload = await _blob.UploadAsync(request.FileName, request.FileBlob, request.ContentType, ct);

        var newEntity = new FileMaster
        {
            Id = Guid.NewGuid().ToString(),
            FileName = request.FileName,
            FileLength = upload.Length,
            ContentType = upload.ContentType,
            FileBlob = null,
            Properties = upload.BlobName,
            PublicURL = upload.Url,
            RegistDate = DateTime.UtcNow,
            Condition = new AuditableEntity { CreatedBy = request.ActorId, RegistDate = DateTime.UtcNow }
        };

        await _context.FileMasters.AddAsync(newEntity, ct);

        source.Condition.IsEnabled = false;
        source.Condition.Status = ConditionStatus.Disabled;
        source.Condition.DeletedBy = request.ActorId;
        source.Condition.DeletedDate = DateTime.UtcNow;
        await _blob.DeleteAsync(source.Properties ?? string.Empty, ct);

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("File replaced: old {OldId} disabled, new {NewId} blob {Blob}", source.Id, newEntity.Id, upload.BlobName);
        result.SetSuccess(1, newEntity);
        return result;
    }
}

public class DeleteFileMasterCommandHandler : IBridgeHandler<DeleteFileMasterCommand, ReturnValues<FileMaster>>
{
    private readonly DefaultContext _context;
    private readonly IAzureBlobFileService _blob;
    private readonly ILogger<DeleteFileMasterCommand> _logger;

    public DeleteFileMasterCommandHandler(ILogger<DeleteFileMasterCommand> logger, DefaultContext db, IAzureBlobFileService blob) : base()
    {
        _context = db;
        _blob = blob;
        _logger = logger;
    }

    public async Task<ReturnValues<FileMaster>> HandleAsync(DeleteFileMasterCommand request, CancellationToken ct)
    {
        var result = new ReturnValues<FileMaster>();

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

        var entity = await _context.FileMasters.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null)
        {
            result.SetError("File not found");
            return result;
        }

        entity.Condition.IsEnabled = false;
        entity.Condition.Status = ConditionStatus.Disabled;
        entity.Condition.DeletedBy = request.DeletedBy;
        entity.Condition.DeletedDate = DateTime.UtcNow;
        await _blob.DeleteAsync(entity.Properties ?? string.Empty, ct);
        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("FileMaster disabled {Id} and blob removed", entity.Id);
        result.SetSuccess(1, entity);
        return result;
    }
}

public class GetFileMasterQueryHandler : IBridgeHandler<GetFileMasterQuery, ReturnValues<FileMaster>>
{
    private readonly DefaultContext _context;

    public GetFileMasterQueryHandler(DefaultContext db) : base()
    {
        _context = db;
    }

    public async Task<ReturnValues<FileMaster>> HandleAsync(GetFileMasterQuery request, CancellationToken ct)
    {
        var result = new ReturnValues<FileMaster>();

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            result.SetError("Id required");
            return result;
        }

        var entity = await _context.FileMasters.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id && x.Condition.IsEnabled && x.Condition.Status == ConditionStatus.Active, ct);
        if (entity is null)
        {
            result.SetError("File not found");
            return result;
        }

        result.SetSuccess(1, entity);
        return result;
    }
}

public static class FileMasterHandlers
{
    public static async Task<FileMasterResult> Handle(CreateFileMasterCommand command, DefaultContext db, IAzureBlobFileService blob, ILogger<CreateFileMasterCommand> logger, CancellationToken ct)
    {
        var bridge = await new CreateFileMasterCommandHandler(logger, db, blob).HandleAsync(command, ct);
        return new FileMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<FileMasterResult> Handle(ReplaceFileMasterCommand command, DefaultContext db, IAzureBlobFileService blob, ILogger<ReplaceFileMasterCommand> logger, CancellationToken ct)
    {
        var bridge = await new ReplaceFileMasterCommandHandler(logger, db, blob).HandleAsync(command, ct);
        return new FileMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<FileMasterResult> Handle(DeleteFileMasterCommand command, DefaultContext db, IAzureBlobFileService blob, ILogger<DeleteFileMasterCommand> logger, CancellationToken ct)
    {
        var bridge = await new DeleteFileMasterCommandHandler(logger, db, blob).HandleAsync(command, ct);
        return new FileMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }

    public static async Task<FileMasterResult> Handle(GetFileMasterQuery query, DefaultContext db, CancellationToken ct)
    {
        var bridge = await new GetFileMasterQueryHandler(db).HandleAsync(query, ct);
        return new FileMasterResult(bridge.Success, bridge.Message, bridge.Data);
    }
}
