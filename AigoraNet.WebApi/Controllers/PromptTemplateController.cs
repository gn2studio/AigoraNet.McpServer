using AigoraNet.Common;
using AigoraNet.Common.CQRS.Prompts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("system/prompt-templates")]
[Authorize(Roles = "Admin")]
public class PromptTemplateController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromptTemplateCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreatePromptTemplateCommand> logger, CancellationToken ct)
    {
        var result = await PromptTemplateHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Template) : BadRequest(result.Error);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePromptTemplateCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdatePromptTemplateCommand> logger, CancellationToken ct)
    {
        var result = await PromptTemplateHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Template) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeletePromptTemplateCommand> logger, CancellationToken ct)
    {
        var result = await PromptTemplateHandlers.Handle(new DeletePromptTemplateCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Template) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await PromptTemplateHandlers.Handle(new GetPromptTemplateQuery(id), db, ct);
        return result.Success ? Ok(result.Template) : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? locale, [FromQuery] string? name, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await PromptTemplateHandlers.Handle(new ListPromptTemplatesQuery(locale, name), db, ct);
        return Ok(result.Templates);
    }
}
