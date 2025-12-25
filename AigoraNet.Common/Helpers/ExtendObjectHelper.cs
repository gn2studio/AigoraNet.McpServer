using Microsoft.AspNetCore.Http;

namespace AigoraNet.Common.Helpers;

public static class ExtendObjectHelper
{
    public static string GetExtensionFromMime(this IFormFile formFile)
    {
        string? mime = formFile.ContentType;

        if (string.IsNullOrWhiteSpace(mime)) return ".bin";

        // 간단한 매핑 (필요하면 확장 추가)
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "image/png", ".png" },
            { "image/jpeg", ".jpg" },
            { "image/jpg", ".jpg" }, // 중복이지만 허용
            { "image/gif", ".gif" },
            { "image/webp", ".webp" },
            { "image/svg+xml", ".svg" },
            { "application/pdf", ".pdf" },
            { "text/plain", ".txt" },
            { "application/zip", ".zip" }
        };

        if (map.TryGetValue(mime, out var ext)) return ext;

        // mime 타입이 "image/xxx" 형태이면 마지막 토큰을 확장자로 사용 시도
        if (mime.Contains("/"))
        {
            var sub = mime.Split('/')[1];
            // 예: "jpeg" -> ".jpg"로 교정
            if (sub.Equals("jpeg", StringComparison.OrdinalIgnoreCase)) return ".jpg";

            // 기타 다른 서브타입 (예: "vnd.ms-excel")은 .ext 형태로 반환
            var safeSub = sub.Split(';')[0]; // 인코딩/매개변수 부분 제거
            return "." + safeSub;
        }

        return ".bin";
    }
}
