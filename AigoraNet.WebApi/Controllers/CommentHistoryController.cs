using AigoraNet.Common.CQRS.Comments;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("private/comment-history")]
[Authorize(Roles = "Admin,User")]
public class CommentHistoryController : DefaultController
{
    private readonly ILogger<CommentHistoryController> _logger;

    public CommentHistoryController(ILogger<CommentHistoryController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] UpsertCommentHistoryCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery] string commentId, [FromQuery] string ownerId, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new RemoveCommentHistoryCommand(commentId, ownerId), ct);
        return ApiResult(result);
    }
}
