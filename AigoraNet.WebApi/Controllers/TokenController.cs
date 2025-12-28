using AigoraNet.Common;
using AigoraNet.Common.CQRS.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/tokens")]
public class TokenController : DefaultController
{
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<RevokeTokenCommand> logger, CancellationToken ct)
    {
        var result = await TokenHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Token) : BadRequest(result.Error);
    }

    [HttpGet("{tokenKey}")]
    public async Task<IActionResult> Get(string tokenKey, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await TokenHandlers.Handle(new GetTokenQuery(tokenKey), db, ct);
        return result.Success ? Ok(result.Token) : NotFound(result.Error);
    }

    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> ListByMember(string memberId, [FromQuery] bool includeExpired, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await TokenHandlers.Handle(new ListTokensByMemberQuery(memberId, includeExpired), db, ct);
        return result.Success ? Ok(result.Tokens) : BadRequest(result.Error);
    }
}
