using AigoraNet.Common;
using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AigoraNet.WebApi.Middleware;

namespace AigoraNet.WebApi.Controllers;

/// <summary>
/// 회원 생성, 조회, 수정, 삭제 및 자기정보 조회/수정 API.
/// 관리자용 시스템 경로와 공개/개인 경로를 구분합니다.
/// </summary>
/// <remarks>
/// - 관리자 전용: /system/members 경로 (Admin 권한 필요)
/// - 공개 가입: POST /auth/register
/// - 사용자 자기 정보: /private/members/me (로그인 토큰 필요)
/// </remarks>
[ApiController]
[Route("system/members")]
public class MemberController : DefaultController
{
    /// <summary>
    /// 관리자가 새 회원을 생성합니다. 이미 존재하는 이메일은 실패합니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "Email": "user@example.com",
    ///   "PasswordHash": "hash",
    ///   "Nickname": "닉네임",
    ///   "MemberType": "User",
    ///   "CreatedBy": "admin"
    /// }
    /// </remarks>
    /// <param name="command">회원 이메일, 비밀번호 해시, 닉네임 등 필수 정보.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>생성된 회원 정보.</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    /// <summary>
    /// 공개 회원가입 엔드포인트입니다. 토큰 없이 호출 가능합니다.
    /// </summary>
    /// <remarks>
    /// - 이메일 중복 시 400을 반환합니다.
    /// - 비밀번호는 이미 해시된 값이어야 합니다.
    /// </remarks>
    /// <param name="command">회원 이메일, 비밀번호 해시, 닉네임 등 필수 정보.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>생성된 회원 정보.</returns>
    [HttpPost("~/auth/register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    /// <summary>
    /// 관리자가 회원 정보를 수정합니다.
    /// </summary>
    /// <remarks>
    /// - Id는 필수입니다.
    /// - 이메일 변경 시 중복 여부를 확인합니다.
    /// </remarks>
    /// <param name="command">수정할 회원 정보(이메일, 닉네임 등).</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>수정된 회원 정보.</returns>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateMemberCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<UpdateMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Member) : BadRequest(result.Error);
    }

    /// <summary>
    /// 로그인한 사용자가 자신의 정보를 수정합니다. 토큰의 MemberId를 강제 주입합니다.
    /// </summary>
    /// <remarks>
    /// - 클라이언트가 Id를 보내더라도 서버에서 토큰의 MemberId로 대체합니다.
    /// - 토큰이 없거나 무효하면 401을 반환합니다.
    /// </remarks>
    /// <param name="command">수정할 회원 정보.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>수정된 본인 정보.</returns>
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

    /// <summary>
    /// 관리자가 회원을 삭제(또는 비활성화)합니다.
    /// </summary>
    /// <remarks>
    /// - deletedBy에 작업 주체를 기록합니다.
    /// </remarks>
    /// <param name="id">회원 ID.</param>
    /// <param name="deletedBy">삭제 요청자 ID.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>삭제된 회원 정보 또는 오류.</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, [FromServices] DefaultContext db, [FromServices] ILogger<DeleteMemberCommand> logger, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new DeleteMemberCommand(id, deletedBy), db, logger, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }

    /// <summary>
    /// 관리자가 특정 회원을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - Id가 없거나 없는 회원이면 404를 반환합니다.
    /// </remarks>
    /// <param name="id">회원 ID.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>회원 정보 또는 404.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get(string id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new GetMemberQuery(id), db, ct);
        return result.Success ? Ok(result.Member) : NotFound(result.Error);
    }

    /// <summary>
    /// 조건(회원 유형)으로 회원 목록을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - type 파라미터를 생략하면 모든 유형을 반환합니다.
    /// - 대량 데이터 시 페이지네이션 확장이 필요할 수 있습니다.
    /// </remarks>
    /// <param name="type">회원 유형 필터(선택).</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>회원 목록.</returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] Member.MemberType? type, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await MemberHandlers.Handle(new ListMembersQuery(type), db, ct);
        return Ok(result.Members);
    }

    /// <summary>
    /// 로그인한 사용자의 프로필을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - 토큰이 없으면 401, 무효하면 401/403을 반환합니다.
    /// </remarks>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>본인 회원 정보 또는 401/404.</returns>
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
