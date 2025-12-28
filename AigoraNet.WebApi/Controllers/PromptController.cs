using AigoraNet.Common;
using AigoraNet.Common.CQRS;
using AigoraNet.Common.CQRS.Prompts;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PromptController : DefaultController
{
    [HttpPost("match")]
    public async Task<IActionResult> Match([FromBody] GetPromptRequest request, [FromServices] DefaultContext db, [FromServices] IPromptCache cache, [FromServices] ILogger<GetPromptByKeywordQuery> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Requirement))
        {
            return BadRequest("Requirement is required");
        }

        var result = await GetPromptByKeywordHandler.Handle(
            new GetPromptByKeywordQuery(request.Requirement, request.Locale, request.AllowRegex),
            db,
            cache,
            logger,
            ct);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

public record GetPromptRequest(string Requirement, string? Locale = null, bool AllowRegex = true);
