using AigoraNet.Common;
using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AigoraNet.WebApi.Middleware;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("system/members")]
public class MemberController : DefaultController
{
    // Admin-create member (system scope)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    // Public registration
    [HttpPost("~/auth/register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    // Self update
    [HttpPut("~/private/members/me")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateMemberCommand> logger, CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue(TokenValidationMiddleware.HttpContextMemberIdKey, out var memberIdObj) || memberIdObj is not string memberId || string.IsNullOrWhiteSpace(memberId))
        {
            return Unauthorized();
        }
        var updatedCommand = command with { Id = memberId };
        var result = await MemberHandlers.Handle(updatedCommand, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new DeleteMemberCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new GetMemberQuery(id), db, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] Member.MemberType? type, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new ListMembersQuery(type), db, ct);
        return Ok(result.Members);
    }

    // Self profile
    [HttpGet("~/private/members/me")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetMe([FromServices] DefaultContext db, CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue(TokenValidationMiddleware.HttpContextMemberIdKey, out var memberIdObj) || memberIdObj is not string memberId || string.IsNullOrWhiteSpace(memberId))
        {
            return Unauthorized();
        }
        var result = await MemberHandlers.Handle(new GetMemberQuery(memberId), db, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }
}
