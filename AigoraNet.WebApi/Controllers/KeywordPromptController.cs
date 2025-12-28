using AigoraNet.Common;
using AigoraNet.Common.CQRS.Prompts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("system/keyword-prompts")]
[Authorize(Roles = "Admin")]
public class KeywordPromptController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertKeywordPromptCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpsertKeywordPromptCommand> logger, CancellationToken ct)
    {
        var result = await KeywordPromptHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.KeywordPrompt) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteKeywordPromptCommand> logger, CancellationToken ct)
    {
        var result = await KeywordPromptHandlers.Handle(new DeleteKeywordPromptCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.KeywordPrompt) : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? locale, [FromQuery] string? promptTemplateId, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await KeywordPromptHandlers.Handle(new ListKeywordPromptsQuery(locale, promptTemplateId), db, ct);
        return Ok(result.Items);
    }
}
