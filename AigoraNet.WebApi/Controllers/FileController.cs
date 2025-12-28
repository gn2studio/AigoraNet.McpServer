using AigoraNet.Common;
using AigoraNet.Common.CQRS.Files;
using AigoraNet.Common.Abstracts;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFileMasterCommand command, [FromServices] DefaultContext db, [FromServices] IAzureBlobFileService blob, [FromServices] ILogger<CreateFileMasterCommand> logger, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(command, db, blob, logger, ct);
        return result.Success ? Ok(result.File) : BadRequest(result.Error);
    }

    [HttpPut("replace")] // replaces existing file: uploads new, disables old, deletes old blob
    public async Task<IActionResult> Replace([FromBody] ReplaceFileMasterCommand command, [FromServices] DefaultContext db, [FromServices] IAzureBlobFileService blob, [FromServices] ILogger<ReplaceFileMasterCommand> logger, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(command, db, blob, logger, ct);
        return result.Success ? Ok(result.File) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] IAzureBlobFileService blob, [FromServices] ILogger<DeleteFileMasterCommand> logger, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(new DeleteFileMasterCommand(id, deletedBy), db, blob, logger, ct);
        return result.Success ? Ok(result.File) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(new GetFileMasterQuery(id), db, ct);
        return result.Success ? Ok(result.File) : NotFound(result.Error);
    }
}
