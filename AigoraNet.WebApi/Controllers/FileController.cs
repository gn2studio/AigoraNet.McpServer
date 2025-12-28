using AigoraNet.Common;
using AigoraNet.Common.CQRS.Files;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFileMasterCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateFileMasterCommand> logger, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.File) : BadRequest(result.Error);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateFileMasterCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateFileMasterCommand> logger, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.File) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteFileMasterCommand> logger, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(new DeleteFileMasterCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.File) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await FileMasterHandlers.Handle(new GetFileMasterQuery(id), db, ct);
        return result.Success ? Ok(result.File) : NotFound(result.Error);
    }
}
