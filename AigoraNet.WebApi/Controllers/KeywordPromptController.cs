using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.WebApi.Authorization;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("system/keyword-prompts")]
[AdminOnly]
public class KeywordPromptController : DefaultController
{
    private readonly ILogger<KeywordPromptController> _logger;

    public KeywordPromptController(ILogger<KeywordPromptController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertKeywordPromptCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, CancellationToken ct)
    {
        var command = new DeleteKeywordPromptCommand(id, deletedBy);
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? locale, [FromQuery] string? promptTemplateId, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new ListKeywordPromptsQuery(locale, promptTemplateId), ct);
        return ApiResult(result);
    }
}
