using AigoraNet.Common.CQRS.Prompts;
using AigoraNet.WebApi.Authorization;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("system/prompt-templates")]
[AdminOnly]
public class PromptTemplateController : DefaultController
{
    private readonly ILogger<PromptTemplateController> _logger;

    public PromptTemplateController(ILogger<PromptTemplateController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePromptTemplateCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdatePromptTemplateCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, CancellationToken ct)
    {
        var command = new DeletePromptTemplateCommand(id, deletedBy);
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new GetPromptTemplateQuery(id), ct);
        return ApiResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? locale, [FromQuery] string? name, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new ListPromptTemplatesQuery(locale, name), ct);
        return ApiResult(result);
    }
}
