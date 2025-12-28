using AigoraNet.Common;
using AigoraNet.Common.CQRS.Comments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("private/comment-history")]
[Authorize(Roles = "Admin,User")]
public class CommentHistoryController : DefaultController
{
    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertCommentHistoryCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpsertCommentHistoryCommand> logger, CancellationToken ct)
    {
        var result = await CommentHistoryHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.History) : BadRequest(result.Error);
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] string commentId, [FromQuery] string ownerId, [FromServices] DefaultContext db, [FromServices] ILogger<RemoveCommentHistoryCommand> logger, CancellationToken ct)
    {
        var result = await CommentHistoryHandlers.Handle(new RemoveCommentHistoryCommand(commentId, ownerId), db, logger, ct);
        return result.Success ? Ok(result.History) : BadRequest(result.Error);
    }
}
