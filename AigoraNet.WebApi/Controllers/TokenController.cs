using AigoraNet.Common;
using AigoraNet.Common.CQRS.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

/// <summary>
/// 발급된 토큰을 조회하거나 취소(폐기)하는 API. 모든 엔드포인트는 인증이 필요합니다.
/// </summary>
/// <remarks>
/// - 토큰 관리 경로: /auth/tokens
/// - Admin/User 권한으로 접근하며, 토큰 헤더 또는 인증 미들웨어 설정이 필요합니다.
/// </remarks>
[ApiController]
[Route("auth/tokens")]
[Authorize]
public class TokenController : DefaultController
{
    /// <summary>
    /// 토큰을 폐기(Revoked) 상태로 변경합니다. 폐기된 토큰은 더 이상 사용되지 않습니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "TokenKey": "abcdefg...",
    ///   "RevokedBy": "admin"
    /// }
    /// 성공 시 폐기된 토큰 엔티티 반환, 실패 시 400.
    /// </remarks>
    /// <param name="command">폐기할 토큰 키와 요청자 정보.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>폐기된 토큰 정보.</returns>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeTokenCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<RevokeTokenCommand> logger, CancellationToken ct)
    {
        var result = await TokenHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.Token) : BadRequest(result.Error);
    }

    /// <summary>
    /// 토큰 키로 토큰 상세 정보를 조회합니다.
    /// </summary>
    /// <remarks>
    /// - 토큰이 없거나 만료/폐기 상태면 404 또는 오류 메시지를 반환합니다.
    /// </remarks>
    /// <param name="tokenKey">조회할 토큰 키.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>토큰 정보 또는 404.</returns>
    [HttpGet("{tokenKey}")]
    public async Task<IActionResult> Get(string tokenKey, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await TokenHandlers.Handle(new GetTokenQuery(tokenKey), db, ct);
        return result.Success ? Ok(result.Token) : NotFound(result.Error);
    }

    /// <summary>
    /// 특정 회원이 보유한 토큰 목록을 조회합니다.
    /// </summary>
    /// <remarks>
    /// - includeExpired=true 시 만료/폐기 토큰도 함께 반환합니다.
    /// </remarks>
    /// <param name="memberId">회원 ID.</param>
    /// <param name="includeExpired">만료/폐기된 토큰 포함 여부.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>토큰 목록.</returns>
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> ListByMember(string memberId, [FromQuery] bool includeExpired, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await TokenHandlers.Handle(new ListTokensByMemberQuery(memberId, includeExpired), db, ct);
        return result.Success ? Ok(result.Tokens) : BadRequest(result.Error);
    }
}
