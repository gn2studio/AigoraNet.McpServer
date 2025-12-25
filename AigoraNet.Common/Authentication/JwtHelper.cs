using AigoraNet.Common.Configurations;
using AigoraNet.Common.DTO;
using AigoraNet.Common.Entities;
using AigoraNet.Common.Extends;
using AigoraNet.Common.Helpers;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AigoraNet.Common.Authentication;

public class JwtHelper
{
    public static string GenerateJwtToken(MemberDTO user, CurrentSiteConfiguration copilotApiConfiguration, int AppendHour)
    {
        if (user == null)
        {
            throw new Exception("user is null");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(AigoraSecret.JwtAuthKey);
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
            Issuer = copilotApiConfiguration.BaseUrl,
            Audience = copilotApiConfiguration.BaseUrl,
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

    public static MemberDTO ParseJwtAuthToken(string token)
    {
        var result = new MemberDTO();
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(AigoraSecret.JwtAuthKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromMinutes(5),

            // 만료되어도 다른 유효성(서명 등)이 통과하면 Claims를 읽을 수 있게 됨.
            ValidateLifetime = false
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (principal?.Identity is ClaimsIdentity identity)
            {
                result = new MemberDTO
                {
                    NickName = identity.FindFirst(ClaimTypes.Name)?.Value,
                    Email = identity.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                    Photo = identity.FindFirst(ClaimTypes.Thumbprint)?.Value
                };

                string UserType = identity.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(UserType))
                {
                    var convertType = UserType.GetFromString<Member.MemberType>();
                    if (convertType.HasValue)
                    {
                        result.Type = convertType.Value;
                    }
                }

                result.Id = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Current.Error(ex);
        }

        return result;
    }

    public static string GenerateLongToken(string userId, int Days)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new Exception("user is null");
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(AigoraSecret.JwtLongKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, userId),
            }),
                Expires = DateTime.UtcNow.AddDays(Days),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            LogHelper.Current.Error(ex);
            return string.Empty;
        }
    }

    public static string ParseJwtLongToken(string token)
    {
        string result = string.Empty;
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(AigoraSecret.JwtLongKey);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            if (principal?.Identity is ClaimsIdentity identity)
            {
                result = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }
        }
        catch (Exception ex)
        {
            LogHelper.Current.Error(ex);
        }

        return result;
    }
}