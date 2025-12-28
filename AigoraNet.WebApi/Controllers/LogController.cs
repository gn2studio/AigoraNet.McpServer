using AigoraNet.Common;
using AigoraNet.Common.CQRS.Logs;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/logs")]
public class LogController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLogItemCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateLogItemCommand> logger, CancellationToken ct)
    {
        var result = await LogItemHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.LogItem) : BadRequest(result.Error);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await LogItemHandlers.Handle(new GetLogItemQuery(id), db, ct);
        return result.Success ? Ok(result.LogItem) : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? level, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int take, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await LogItemHandlers.Handle(new ListLogItemsQuery(level, from, to, take == 0 ? 100 : take), db, ct);
        return result.Success ? Ok(result.Items) : BadRequest(result.Error);
    }
}
