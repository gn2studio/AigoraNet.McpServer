using AigoraNet.Common.Configurations;
using GN2.Core.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AigoraNet.Common.Authentication;

public static class ExtendAuthentication
{
    public static string GetCurrentUserId(this IHttpContextAccessor httpContextAccessor)
    {
        // 1) 유효성 체크
        var context = httpContextAccessor?.HttpContext;
        if (context?.Request?.Cookies == null)
            return string.Empty;

        // 2) DI 컨테이너에서 설정(ClientConfiguration)과 시크릿 키 가져오기
        var services = context.RequestServices;
        var clientConfig = services.GetRequiredService<ClientConfiguration>();
        var jwtSecret = AigoraSecret.JwtAuthKey; // 만약 JwtKey 역시 DI에 등록했다면 services.GetRequiredService<string>("JwtKey") 처럼 가져오셔도 됩니다.
        var cookieName = clientConfig.AccessToken;

        // 3) 쿠키에서 토큰 추출
        if (!context.Request.Cookies.TryGetValue(cookieName, out var token)
            || string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        try
        {
            // 4) 토큰 핸들러 및 검증 파라미터 설정
            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.ASCII.GetBytes(jwtSecret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            // 5) 토큰 검증 및 ClaimsPrincipal 획득
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            // 6) NameIdentifier 클레임 가져오기
            var userIdClaim = principal.Claims
                                                   .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return string.Empty;

            return userIdClaim.Value;
        }
        catch
        {
            // 검증 실패 등
            return string.Empty;
        }
    }
}
