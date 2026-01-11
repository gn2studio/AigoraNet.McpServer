using AigoraNet.Common.CQRS.Files;
using GN2.Common.Library.Abstracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

/// <summary>
/// 파일 메타데이터와 블랍 저장소를 관리하는 API. 업로드, 교체, 삭제, 조회를 제공합니다.
/// </summary>
/// <remarks>
/// - 경로: /private/files (Admin/User 권한 필요)
/// - Blob 업로드 후 메타데이터가 DB에 저장됩니다.
/// - 교체 시 새 블랍 업로드 후 기존 블랍을 삭제하고 메타데이터를 비활성화합니다.
/// </remarks>
[ApiController]
[Route("private/files")]
[Authorize(Roles = "Admin,User")]
public class FileController : DefaultController
{
    private readonly ILogger<FileController> _logger;

    public FileController(ILogger<FileController> logger, IActionBridge bridge, IObjectLinker linker) : base(bridge, linker)
    {
        _logger = logger;
    }

    /// <summary>
    /// 새 파일을 업로드하고 메타데이터를 등록합니다.
    /// </summary>
    /// <remarks>
    /// 요청 예시:
    /// {
    ///   "FileName": "sample.txt",
    ///   "FileSize": 1234,
    ///   "ContentType": "text/plain",
    ///   "Content": "Base64 혹은 바이너리",
    ///   "CreatedBy": "user"
    /// }
    /// 응답: 저장된 파일 메타데이터.
    /// </remarks>
    /// <param name="command">파일 이름, 크기, MIME 타입, Base64/바이트 내용.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>등록된 파일 메타데이터.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFileMasterCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    /// <summary>
    /// 기존 파일을 새로운 파일로 교체합니다. 새 파일 업로드 후 이전 파일은 비활성화되고 블랍에서 삭제됩니다.
    /// </summary>
    /// <remarks>
    /// - Id와 새 파일 정보가 모두 필요합니다.
    /// - 이전 파일은 Condition.IsEnabled=false로 표시되고 Blob에서 제거됩니다.
    /// </remarks>
    /// <param name="command">교체 대상 파일 ID와 새 파일 정보.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>교체된 새 파일 메타데이터.</returns>
    [HttpPut("replace")] // replaces existing file: uploads new, disables old, deletes old blob
    public async Task<IActionResult> Replace([FromBody] ReplaceFileMasterCommand command, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    /// <summary>
    /// 파일 메타데이터를 삭제(또는 비활성화)하고 블랍 파일을 제거합니다.
    /// </summary>
    /// <remarks>
    /// - deletedBy로 추적 정보를 남깁니다.
    /// - 존재하지 않는 파일이면 404 반환.
    /// </remarks>
    /// <param name="id">삭제할 파일 ID.</param>
    /// <param name="deletedBy">삭제 요청자.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>삭제된 파일 정보 또는 404.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] string deletedBy, CancellationToken ct)
    {
        var command = new DeleteFileMasterCommand(id, deletedBy);
        var result = await _bridge.SendAsync(command, ct);
        return ApiResult(result);
    }

    /// <summary>
    /// 파일 메타데이터를 단건 조회합니다.
    /// </summary>
    /// <remarks>
    /// - 실제 파일 다운로드 URL은 별도 Blob 경로나 SAS 토큰 정책을 따릅니다.
    /// </remarks>
    /// <param name="id">파일 ID.</param>
    /// <param name="ct">요청 취소 토큰.</param>
    /// <returns>파일 메타데이터 또는 404.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, CancellationToken ct)
    {
        var result = await _bridge.SendAsync(new GetFileMasterQuery(id), ct);
        return ApiResult(result);
    }
}
