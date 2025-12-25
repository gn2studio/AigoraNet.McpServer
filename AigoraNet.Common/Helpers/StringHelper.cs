using System.Text.RegularExpressions;

namespace AigoraNet.Common.Helpers;

public static class StringHelper
{
    public static bool IsValidEmail(this string email)
    {
        bool valid = Regex.IsMatch(email, @"[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?");
        return valid;
    }

    public static bool IsValidUrl(this string url)
    {
        return new Regex("^(https?://)?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?(([0-9]{1,3}\\.){3}[0-9]{1,3}|([0-9a-z_!~*'()-]+\\.)*([0-9a-z][0-9a-z-]{0,61})?[0-9a-z](\\.[a-z]{2,6})?)(:[0-9]{1,5})?((/?)|(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$").IsMatch(url);
    }

    public static string GetYoutubeThumbnailURL(this string code)
    {
        return $"https://img.youtube.com/vi/{code}/mqdefault.jpg";
    }

    public static Dictionary<string, string> SpaceSplitList(this string originText)
    {
        var result = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(originText))
        {
            var parts = originText.Split(' ');
            foreach (string part in parts)
            {
                result.Add(part, part);
            }
        }

        return result;
    }
}