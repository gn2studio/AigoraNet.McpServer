using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using AigoraNet.WebApi.Middleware;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

/// <summary>
/// 회원 등록, 조회, 수정, 삭제 및 자기정보 조회/수정 API.
/// 관리자와 시스템 내의 등록/삭제 흐름을 제공합니다.
/// </summary>
/// <remarks>
/// - 관리자용 엔드포인트: /system/members 하위 (Admin 권한 필요)
/// - 회원 가입: POST /auth/register
/// - 본인 정보 수정/조회: /private/members/me (로그인 토큰 필요)
/// </remarks>
[ApiController]
[Route("system/members")]
public class MemberController : DefaultController
{
    private readonly ILogger<MemberController> _logger;

    public MemberController(
        ILogger<MemberController> logger,
        IActionBridge bridge,
        IObjectLinker linker
    ) : base(bridge, linker)
    {
        _logger = logger;
    }


    /// <summary>
    /// 관리자가 새 회원을 등록합니다.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateMemberCommand command, CancellationToken ct)
    {
        ReturnValues<MemberDTO> result = new();
        ReturnValues<Member> rst = await _bridge.SendAsync(command, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 외부 사용자가 회원 가입을 진행합니다.
    /// </summary>
    [HttpPost("~/auth/register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateMemberCommand command, CancellationToken ct)
    {
        ReturnValues<MemberDTO> result = new();
        ReturnValues<Member> rst = await _bridge.SendAsync(command, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 관리자가 회원 정보를 수정합니다.
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] UpdateMemberCommand command, CancellationToken ct)
    {
        ReturnValues<MemberDTO> result = new();
        ReturnValues<Member> rst = await _bridge.SendAsync(command, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 로그인한 사용자가 자신의 정보를 수정합니다.
    /// </summary>
    [HttpPut("~/private/members/me")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> UpdateMe([FromBody] MemberInfoDTO dto, CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue(TokenValidationMiddleware.HttpContextMemberIdKey, out var memberIdObj) || memberIdObj is not string memberId || string.IsNullOrWhiteSpace(memberId))
        {
            return Unauthorized();
        }

        var command = new UpdateMemberCommand(dto.Id, dto.NickName, dto.Photo, dto.Bio, memberId);
        ReturnValues<MemberDTO> result = new();

        ReturnValues<Member> rst = await _bridge.SendAsync(command, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 관리자가 회원을 비활성화합니다.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue(TokenValidationMiddleware.HttpContextMemberIdKey, out var memberIdObj) || memberIdObj is not string memberId || string.IsNullOrWhiteSpace(memberId))
        {
            return Unauthorized();
        }

        var command = new DeleteMemberCommand(id, memberId);
        ReturnValues<MemberDTO> result = new();

        ReturnValues<Member> rst = await _bridge.SendAsync(command, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 특정 회원을 조회합니다.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Get([FromRoute] string id, CancellationToken ct)
    {
        ReturnValues<MemberDTO> result = new();
        var query = new GetMemberQuery(id);
        ReturnValues<Member> rst = await _bridge.SendAsync(query, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 회원 목록을 조회합니다.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> List([FromQuery] SearchParameter? search, CancellationToken ct)
    {
        ReturnValues<List<MemberDTO>> result = new();
        if (search == null) search = new SearchParameter();
        var query = new GetMembersQuery(search);
        ReturnValues<List<Member>> rst = await _bridge.SendAsync(query, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<List<Member>, List<MemberDTO>>(rst.Data);
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 로그인한 사용자의 정보를 조회합니다.
    /// </summary>
    [HttpGet("~/private/members/me")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        if (!HttpContext.Items.TryGetValue(TokenValidationMiddleware.HttpContextMemberIdKey, out var memberIdObj) || memberIdObj is not string memberId || string.IsNullOrWhiteSpace(memberId))
        {
            return Unauthorized();
        }

        ReturnValues<MemberDTO> result = new();
        var query = new GetMemberQuery(memberId);
        ReturnValues<Member> rst = await _bridge.SendAsync(query, ct);
        if (rst.Success && rst.Data != null)
        {
            result.Success = true;
            result.Data = _linker.Map<Member, MemberDTO>(rst.Data);
        }

        return ApiResult(result);
    }
}
