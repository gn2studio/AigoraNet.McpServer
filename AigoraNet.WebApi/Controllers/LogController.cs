using AigoraNet.Common;
using AigoraNet.Common.CQRS.Logs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

/// <summary>
/// 애플리케이션 로그를 수집/조회하는 관리자용 API.
/// </summary>
/// <remarks>
/// - 경로: /system/logs (Admin 전용)
/// - 외부 시스템이 운영 로그를 적재하거나, 백오피스에서 조회할 때 사용합니다.
/// </remarks>
[ApiController]
[Route("system/logs")]
[Authorize(Roles = "Admin")]
public class LogController : DefaultController
{
    /// <summary>
    /// 새 로그 항목을 기록합니다. 외부 시스템이나 백오피스에서 수동으로 적재할 때 사용합니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "Level": "Error",
    ///   "Message": "에러 메시지",
    ///   "Context": "추가 컨텍스트 JSON",
    ///   "CreatedBy": "admin"
    /// }
    /// </remarks>
    /// <param name="command">로그 레벨, 메시지, 컨텍스트 데이터.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="logger">구조적 로깅용 로거.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>저장된 로그 항목.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLogItemCommand command, [FromServices] DefaultContext db, [FromServices] ILogger<CreateLogItemCommand> logger, CancellationToken ct)
    {
        var result = await LogItemHandlers.Handle(command, db, logger, ct);
        return result.Success ? Ok(result.LogItem) : BadRequest(result.Error);
    }

    /// <summary>
    /// 로그 ID로 단건 조회합니다.
    /// </summary>
    /// <remarks>
    /// - 존재하지 않는 ID면 404를 반환합니다.
    /// </remarks>
    /// <param name="id">로그 ID (long).</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>로그 항목 또는 404.</returns>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await LogItemHandlers.Handle(new GetLogItemQuery(id), db, ct);
        return result.Success ? Ok(result.LogItem) : NotFound(result.Error);
    }

    /// <summary>
    /// 로그를 필터 조건으로 조회합니다.
    /// </summary>
    /// <remarks>
    /// - level: "Error", "Warning" 등 문자열 레벨 필터.
    /// - from/to: UTC 기준 시간 필터.
    /// - take: 최대 반환 개수(0이면 100으로 대체).
    /// </remarks>
    /// <param name="level">로그 레벨 필터(예: Error, Warning).</param>
    /// <param name="from">시작 시각(UTC).</param>
    /// <param name="to">종료 시각(UTC).</param>
    /// <param name="take">가져올 최대 개수. 0이면 100으로 대체.</param>
    /// <param name="db">DB 컨텍스트.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>필터링된 로그 목록.</returns>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? level, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int take, [FromServices] DefaultContext db, CancellationToken ct)
    {
        var result = await LogItemHandlers.Handle(new ListLogItemsQuery(level, from, to, take == 0 ? 100 : take), db, ct);
        return result.Success ? Ok(result.Items) : BadRequest(result.Error);
    }
}
