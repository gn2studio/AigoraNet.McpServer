using AigoraNet.Common.Configurations;
using AigoraNet.Common.DTO;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AigoraNet.Common.Helpers;

public class AuthticationHelper
{
    public static string GenerateJwtToken(MemberDTO user, CurrentSiteConfiguration externalApiConfiguration, int AppendHour)
    {
        if (user == null)
        {
            throw new Exception("user is null");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(AigoraSecret.JwtAuthKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.NickName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Thumbprint, user.Photo ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString() ?? ""),
                new Claim(ClaimTypes.Role, user.Type.ToString() ?? "")
            }),
            Expires = DateTime.UtcNow.AddHours(AppendHour),
            Issuer = externalApiConfiguration.BaseUrl,
            Audience = externalApiConfiguration.BaseUrl,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static string NoExpiredTokenGenerate(MemberDTO userinfo, CurrentSiteConfiguration externalApiConfiguration)
    {
        if (userinfo == null)
        {
            throw new Exception("user is null");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(AigoraSecret.JwtAuthKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, userinfo.NickName ?? ""),
                new Claim(ClaimTypes.Email, userinfo.Email ?? ""),
                new Claim(ClaimTypes.Thumbprint, userinfo.Photo ?? ""),
                new Claim(ClaimTypes.NameIdentifier, userinfo.Id.ToString() ?? ""),
                new Claim(ClaimTypes.Role, userinfo.Type.ToString() ?? "")
            }),
            Expires = DateTime.UtcNow.AddYears(1),
            Issuer = externalApiConfiguration.BaseUrl,
            Audience = externalApiConfiguration.BaseUrl,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public static bool IsExpired(string jwtToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwtToken);
        var exp = token.ValidTo; // UTC 시간 기준
        return exp < DateTime.UtcNow;
    }
}
