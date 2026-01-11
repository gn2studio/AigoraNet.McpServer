using AigoraNet.Common.CQRS.Members;
using AigoraNet.Common.CQRS.Auth;
using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using AigoraNet.WebApi.Authorization;
using AigoraNet.WebApi.Middleware;
using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace AigoraNet.WebApi.Controllers;

public record LoginResponse(string TokenKey, DateTime? ExpiresAt, MemberDTO Member);

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
    private readonly IEmailSender _emailSender;
    private readonly IHostEnvironment _env;

    public MemberController(
        ILogger<MemberController> logger,
        IActionBridge bridge,
        IObjectLinker linker,
        IEmailSender emailSender,
        IHostEnvironment env
    ) : base(bridge, linker)
    {
        _logger = logger;
        _emailSender = emailSender;
        _env = env;
    }


    /// <summary>
    /// 관리자가 새 회원을 등록합니다.
    /// </summary>
    [HttpPost]
    [AdminOnly]
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

            try
            {
                var confirmUrl = BuildConfirmUrl(rst.Data.Id);
                var body = await RenderEmailTemplateAsync("ConfirmEmail.html", new Dictionary<string, string>
                {
                    ["{{Nickname}}"] = command.NickName ?? command.Email,
                    ["{{ConfirmUrl}}"] = confirmUrl
                }, ct);

                var subject = "Please confirm your AigoraNet account";
                await _emailSender.SendEmailAsync(command.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send registration email to {Email}", command.Email);
            }
        }

        return ApiResult(result);
    }

    /// <summary>
    /// 관리자가 회원 정보를 수정합니다.
    /// </summary>
    [HttpPut]
    [AdminOnly]
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
    [AdminOnly]
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
    [AdminOnly]
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
    [AdminOnly]
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

    /// <summary>
    /// 로그인하여 토큰을 발급받고 회원 정보를 조회합니다.
    /// </summary>
    [HttpPost("~/auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto, CancellationToken ct)
    {
        var secret = !string.IsNullOrWhiteSpace(dto.Password) ? dto.Password : "";
        var rst = await _bridge.SendAsync(new LoginCommand(dto.Email, secret ?? string.Empty), ct);
        if (!rst.Success || rst.Data is null)
        {
            return ApiResult(rst);
        }

        var mapped = _linker.Map<Member, MemberDTO>(rst.Data.Member);
        var response = new ReturnValues<LoginResponse>
        {
            Success = true,
            Data = new LoginResponse(rst.Data.TokenKey, rst.Data.ExpiresAt, mapped)
        };

        return ApiResult(response);
    }

    /// <summary>
    /// 이메일 인증을 처리합니다.
    /// </summary>
    [HttpGet("~/auth/confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] string memberId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(memberId))
        {
            return Content(RenderConfirmResult("Invalid request"), "text/html", Encoding.UTF8);
        }

        var confirm = await _bridge.SendAsync(new ConfirmMemberEmailCommand(memberId, memberId), ct);
        if (!confirm.Success)
        {
            return Content(RenderConfirmResult(confirm.Message ?? "Member not found"), "text/html", Encoding.UTF8);
        }

        return Content(RenderConfirmResult("인증되었습니다."), "text/html", Encoding.UTF8);
    }

    private string BuildConfirmUrl(string memberId)
    {
        var scheme = HttpContext?.Request?.Scheme ?? "https";
        var host = HttpContext?.Request?.Host.Value ?? "localhost";
        return $"{scheme}://{host}/auth/confirm-email?memberId={Uri.EscapeDataString(memberId)}";
    }

    private async Task<string> RenderEmailTemplateAsync(string fileName, IDictionary<string, string> tokens, CancellationToken ct)
    {
        var path = Path.Combine(_env.ContentRootPath, "EmailTemplates", fileName);
        var html = await System.IO.File.ReadAllTextAsync(path, ct);
        foreach (var kvp in tokens)
        {
            html = html.Replace(kvp.Key, kvp.Value);
        }
        return html;
    }

    private static string RenderConfirmResult(string message)
    {
        return $"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>이메일 인증</title>
</head>
<body style="font-family:Arial,sans-serif; text-align:center; padding:40px;">
    <h2>{message}</h2>
    <button onclick="window.close();" style="padding:10px 20px; font-size:16px;">닫기</button>
</body>
</html>
""";
    }
}
