using AigoraNet.Common;
using AigoraNet.Common.CQRS.Boards;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("api/boards")]
public class BoardController : DefaultController
{
    [HttpPost("masters")]
    public async Task<IActionResult> CreateMaster([FromBody] CreateBoardMasterCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateBoardMasterCommand> logger, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Master) : BadRequest(result.Error);
    }

    [HttpPut("masters")]
    public async Task<IActionResult> UpdateMaster([FromBody] UpdateBoardMasterCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateBoardMasterCommand> logger, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Master) : BadRequest(result.Error);
    }

    [HttpDelete("masters/{id}")]
    public async Task<IActionResult> DeleteMaster(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteBoardMasterCommand> logger, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(new DeleteBoardMasterCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Master) : NotFound(result.Error);
    }

    [HttpGet("masters/{id}")]
    public async Task<IActionResult> GetMaster(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(new GetBoardMasterQuery(id), db, ct);
        return result.Success ? Ok(result.Master) : NotFound(result.Error);
    }

    [HttpGet("masters")]
    public async Task<IActionResult> ListMasters([FromQuery] string? section, [FromQuery] string? site, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(new ListBoardMastersQuery(section, site), db, ct);
        return Ok(result.Masters);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBoardCategoryCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateBoardCategoryCommand> logger, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Category) : BadRequest(result.Error);
    }

    [HttpPut("categories")]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateBoardCategoryCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateBoardCategoryCommand> logger, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Category) : BadRequest(result.Error);
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(string id, [FromQuery] string deletedBy, [FromQuery] bool force, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteBoardCategoryCommand> logger, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(new DeleteBoardCategoryCommand(id, deletedBy, force), db, logger, ct);
        return result.Success ? Ok(result.Category) : BadRequest(result.Error);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> ListCategories([FromQuery] string boardMasterId, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(new ListBoardCategoriesQuery(boardMasterId), db, ct);
        return result.Success ? Ok(result.Categories) : BadRequest(result.Error);
    }

    [HttpPost("contents")]
    public async Task<IActionResult> CreateContent([FromBody] CreateBoardContentCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateBoardContentCommand> logger, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Content) : BadRequest(result.Error);
    }

    [HttpPut("contents")]
    public async Task<IActionResult> UpdateContent([FromBody] UpdateBoardContentCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateBoardContentCommand> logger, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Content) : BadRequest(result.Error);
    }

    [HttpDelete("contents/{id}")]
    public async Task<IActionResult> DeleteContent(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteBoardContentCommand> logger, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(new DeleteBoardContentCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Content) : BadRequest(result.Error);
    }

    [HttpGet("contents/{id}")]
    public async Task<IActionResult> GetContent(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(new GetBoardContentQuery(id), db, ct);
        return result.Success ? Ok(result.Content) : NotFound(result.Error);
    }

    [HttpGet("contents")]
    public async Task<IActionResult> ListContents([FromQuery] string masterId, [FromQuery] string? categoryId, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(new ListBoardContentsQuery(masterId, categoryId), db, ct);
        return result.Success ? Ok(result.Items) : BadRequest(result.Error);
    }
}
