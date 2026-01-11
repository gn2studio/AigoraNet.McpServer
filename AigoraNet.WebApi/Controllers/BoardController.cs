using AigoraNet.Common.CQRS.Boards;
using AigoraNet.WebApi.Authorization;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("public/boards")]
public class BoardController : DefaultController
{
    private readonly ILogger<BoardController> _logger;

    public BoardController(ILogger<BoardController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    [HttpPost("masters")]
    [AdminOnly]
    public async Task<IActionResult> CreateMaster([FromBody] CreateBoardMasterCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpPut("masters")]
    [AdminOnly]
    public async Task<IActionResult> UpdateMaster([FromBody] UpdateBoardMasterCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete("masters/{id}")]
    [AdminOnly]
    public async Task<IActionResult> DeleteMaster(string id, [FromQuery] string deletedBy, CancellationToken ct)
    {
        var command = new DeleteBoardMasterCommand(id, deletedBy);
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpGet("masters/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMaster(string id, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new GetBoardMasterQuery(id), ct);
        return ApiResult(result);
    }

    [HttpGet("masters")]
    [AllowAnonymous]
    public async Task<IActionResult> ListMasters([FromQuery] string? section, [FromQuery] string? site, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new ListBoardMastersQuery(section, site), ct);
        return ApiResult(result);
    }

    [HttpPost("categories")]
    [AdminOnly]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBoardCategoryCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpPut("categories")]
    [AdminOnly]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateBoardCategoryCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete("categories/{id}")]
    [AdminOnly]
    public async Task<IActionResult> DeleteCategory(string id, [FromQuery] string deletedBy, [FromQuery] bool force, CancellationToken ct)
    {
        var command = new DeleteBoardCategoryCommand(id, deletedBy, force);
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> ListCategories([FromQuery] string boardMasterId, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new ListBoardCategoriesQuery(boardMasterId), ct);
        return ApiResult(result);
    }

    [HttpPost("contents")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> CreateContent([FromBody] CreateBoardContentCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpPut("contents")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> UpdateContent([FromBody] UpdateBoardContentCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpDelete("contents/{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> DeleteContent(string id, [FromQuery] string deletedBy, CancellationToken ct)
    {
        var command = new DeleteBoardContentCommand(id, deletedBy);
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    [HttpGet("contents")]
    [AllowAnonymous]
    public async Task<IActionResult> ListContents([FromQuery] string masterId, [FromQuery] string? categoryId, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new ListBoardContentsQuery(masterId, categoryId), ct);
        return ApiResult(result);
    }
}
