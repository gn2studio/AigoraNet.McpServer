using AigoraNet.Common;
using AigoraNet.Common.CQRS.Comments;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateCommentCommand> logger, CancellationToken ct)
    {
        var result = await CommentHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Comment) : BadRequest(result.Error);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCommentCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateCommentCommand> logger, CancellationToken ct)
    {
        var result = await CommentHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Comment) : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteCommentCommand> logger, CancellationToken ct)
    {
        var result = await CommentHandlers.Handle(new DeleteCommentCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Comment) : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string key, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await CommentHandlers.Handle(new ListCommentsQuery(key), db, ct);
        return result.Success ? Ok(result.Comments) : BadRequest(result.Error);
    }
}
