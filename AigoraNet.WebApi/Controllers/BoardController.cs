using AigoraNet.Common;
using AigoraNet.Common.CQRS.Boards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

/// <summary>
/// 게시판 마스터/카테고리/게시글을 관리하고 조회하는 API.
/// 관리자용 보호 엔드포인트와 공개 열람 엔드포인트를 구분해서 제공합니다.
/// </summary>
/// <remarks>
/// 사용 가이드:
/// - 관리 영역: /public/boards/masters, /public/boards/categories, /public/boards/contents (Admin 권한 필요)
/// - 공개 조회: GET /public/boards/masters, GET /public/boards/masters/{id}, GET /public/boards/contents 등은 인증 없이 조회 가능
/// - 토큰 헤더: 관리자/사용자 보호 엔드포인트 호출 시 인증 토큰을 포함해야 합니다.
/// </remarks>
[ApiController]
[Route("public/boards")]
public class BoardController : DefaultController
{
    /// <summary>
    /// 게시판 마스터를 생성합니다. 동일 섹션/사이트 내 중복이 없도록 호출 전 검증이 필요합니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "Section": "notice",
    ///   "Site": "main",
    ///   "Name": "공지사항",
    ///   "CreatedBy": "admin"
    /// }
    /// 응답: 생성된 마스터 객체(JSON). 실패 시 400에 에러 메시지 반환.
    /// </remarks>
    /// <param name="command">게시판 기본 정보(섹션, 사이트, 이름 등).</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>생성된 게시판 마스터.</returns>
    [HttpPost("masters")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateMaster([FromBody] CreateBoardMasterCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateBoardMasterCommand> logger, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Master) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시판 마스터 정보를 수정합니다.
    /// </summary>
    /// <remarks>
    /// - Id 필드는 필수입니다.
    /// - 섹션/사이트가 변경되면 해당 영역 접근 정책을 재점검하세요.
    /// </remarks>
    /// <param name="command">수정할 게시판 마스터 정보.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>갱신된 게시판 마스터.</returns>
    [HttpPut("masters")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMaster([FromBody] UpdateBoardMasterCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateBoardMasterCommand> logger, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Master) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시판 마스터를 삭제(또는 비활성화)합니다.
    /// </summary>
    /// <remarks>
    /// - 연관 카테고리/게시글이 있을 경우 삭제 정책(소프트/하드)에 유의하세요.
    /// - deletedBy에 작업 주체를 남겨 추적합니다.
    /// </remarks>
    /// <param name="id">삭제할 게시판 마스터 ID.</param>
    /// <param name="deletedBy">삭제 요청자 ID.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>삭제된 게시판 마스터 또는 404.</returns>
    [HttpDelete("masters/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMaster(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteBoardMasterCommand> logger, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(new DeleteBoardMasterCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Master) : NotFound(result.Error);
    }

    /// <summary>
    /// 게시판 마스터 단건을 조회합니다.
    /// </summary>
    /// <remarks>
    /// 공개 엔드포인트로, 섹션/사이트에 상관없이 ID 기준 조회가 가능합니다.
    /// </remarks>
    /// <param name="id">조회할 게시판 마스터 ID.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>게시판 마스터 또는 404.</returns>
    [HttpGet("masters/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMaster(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(new GetBoardMasterQuery(id), db, ct);
        return result.Success ? Ok(result.Master) : NotFound(result.Error);
    }

    /// <summary>
    /// 섹션/사이트 조건에 맞는 게시판 마스터 목록을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - section, site는 선택 필터입니다.
    /// - 둘 다 비우면 전체 목록을 반환합니다.
    /// </remarks>
    /// <param name="section">섹션 코드(선택).</param>
    /// <param name="site">사이트 코드(선택).</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>게시판 마스터 목록.</returns>
    [HttpGet("masters")]
    [AllowAnonymous]
    public async Task<IActionResult> ListMasters([FromQuery] string? section, [FromQuery] string? site, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardMasterHandlers.Handle(new ListBoardMastersQuery(section, site), db, ct);
        return Ok(result.Masters);
    }

    /// <summary>
    /// 게시판 카테고리를 생성합니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "BoardMasterId": "master-id",
    ///   "Name": "FAQ",
    ///   "CreatedBy": "admin"
    /// }
    /// </remarks>
    /// <param name="command">카테고리 정보.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>생성된 카테고리.</returns>
    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateBoardCategoryCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateBoardCategoryCommand> logger, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Category) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시판 카테고리를 수정합니다.
    /// </summary>
    /// <remarks>
    /// - 카테고리 이름, 노출 여부 등을 변경할 때 사용합니다.
    /// </remarks>
    /// <param name="command">카테고리 수정 정보.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>갱신된 카테고리.</returns>
    [HttpPut("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateBoardCategoryCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateBoardCategoryCommand> logger, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Category) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시판 카테고리를 삭제하거나 강제 삭제합니다.
    /// </summary>
    /// <remarks>
    /// - force=true이면 연관 데이터가 있어도 강제 삭제합니다. 데이터 손실에 유의하십시오.
    /// </remarks>
    /// <param name="id">카테고리 ID.</param>
    /// <param name="deletedBy">삭제 요청자 ID.</param>
    /// <param name="force">연관 데이터가 있을 때도 강제 삭제할지 여부.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>삭제 결과.</returns>
    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(string id, [FromQuery] string deletedBy, [FromQuery] bool force, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteBoardCategoryCommand> logger, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(new DeleteBoardCategoryCommand(id, deletedBy, force), db, logger, ct);
        return result.Success ? Ok(result.Category) : BadRequest(result.Error);
    }

    /// <summary>
    /// 특정 게시판 마스터에 속한 카테고리 목록을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - boardMasterId는 필수입니다.
    /// - 카테고리가 없으면 빈 배열을 반환합니다.
    /// </remarks>
    /// <param name="boardMasterId">게시판 마스터 ID.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>카테고리 목록.</returns>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> ListCategories([FromQuery] string boardMasterId, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardCategoryHandlers.Handle(new ListBoardCategoriesQuery(boardMasterId), db, ct);
        return result.Success ? Ok(result.Categories) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시글을 작성합니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "BoardMasterId": "master-id",
    ///   "BoardCategoryId": "category-id",
    ///   "Title": "제목",
    ///   "Content": "본문 내용",
    ///   "CreatedBy": "user"
    /// }
    /// </remarks>
    /// <param name="command">게시글 본문, 제목, 카테고리 등 필수 정보.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>작성된 게시글.</returns>
    [HttpPost("contents")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> CreateContent([FromBody] CreateBoardContentCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateBoardContentCommand> logger, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Content) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시글을 수정합니다.
    /// </summary>
    /// <remarks>
    /// - Id는 필수이며 작성자/권한 정책에 따라 실패할 수 있습니다.
    /// </remarks>
    /// <param name="command">수정할 게시글 정보.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>갱신된 게시글.</returns>
    [HttpPut("contents")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> UpdateContent([FromBody] UpdateBoardContentCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateBoardContentCommand> logger, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Content) : BadRequest(result.Error);
    }

    /// <summary>
    /// 게시글을 삭제합니다.
    /// </summary>
    /// <remarks>
    /// - deletedBy로 추적 정보를 남깁니다.
    /// - 권한 부족 시 401/403이 반환됩니다.
    /// </remarks>
    /// <param name="id">게시글 ID.</param>
    /// <param name="deletedBy">삭제 요청자 ID.</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>삭제된 게시글 또는 오류.</returns>
    [HttpDelete("contents/{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> DeleteContent(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteBoardContentCommand> logger, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(new DeleteBoardContentCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Content) : BadRequest(result.Error);
    }

    /// <summary>
    /// 특정 게시판/카테고리의 게시글 목록을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - masterId는 필수, categoryId는 선택입니다.
    /// - 페이지네이션이 필요하면 이후 확장 포인트로 take/skip을 추가하세요.
    /// </remarks>
    /// <param name="masterId">게시판 마스터 ID.</param>
    /// <param name="categoryId">카테고리 ID(선택).</param>
    /// <param name="db">콘텐츠 DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>게시글 목록.</returns>
    [HttpGet("contents")]
    [AllowAnonymous]
    public async Task<IActionResult> ListContents([FromQuery] string masterId, [FromQuery] string? categoryId, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await BoardContentHandlers.Handle(new ListBoardContentsQuery(masterId, categoryId), db, ct);
        return result.Success ? Ok(result.Items) : BadRequest(result.Error);
    }
}
