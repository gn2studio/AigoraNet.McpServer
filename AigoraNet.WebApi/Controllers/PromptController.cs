using AigoraNet.Common.CQRS.Prompts;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("public/[controller]")]
[AllowAnonymous]
public class PromptController : DefaultController
{
    private readonly ILogger<PromptController> _logger;

    public PromptController(ILogger<PromptController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    [HttpPost("match")]
    public async Task<IActionResult> Match([FromBody] GetPromptRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Requirement))
        {
            return BadRequest("Requirement is required");
        }

        var result = await _bridge.SendAsync(new GetPromptByKeywordQuery(request.Requirement, request.Locale, request.AllowRegex), ct);
        return ApiResult(result);
    }
}

public record GetPromptRequest(string Requirement, string? Locale = null, bool AllowRegex = true);
