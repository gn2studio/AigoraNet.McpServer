using AigoraNet.Common;
using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/members")]
public class MemberController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new DeleteMemberCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new GetMemberQuery(id), db, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Member.MemberType? type, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new ListMembersQuery(type), db, ct);
        return Ok(result.Members);
    }
}
