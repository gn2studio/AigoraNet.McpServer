using AigoraNet.Common.CQRS.Comments;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("private/comments")]
[Authorize(Roles = "Admin,User")]
public class CommentController : DefaultController
{
    private readonly ILogger<CommentController> _logger;

    public CommentController(ILogger<CommentController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCommentCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new DeleteCommentCommand(id, deletedBy), ct);
        return ApiResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string key, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new ListCommentsQuery(key), ct);
        return ApiResult(result);
    }
}
