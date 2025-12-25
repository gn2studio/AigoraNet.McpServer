using System.Text.RegularExpressions;

namespace AigoraNet.Common.Helpers;

public static class HtmlTagHelper
{
    public static string ValueCompare(this string? original, string compareString, string returnString)
    {
        return (original != null && original.Equals(compareString, StringComparison.OrdinalIgnoreCase) ? returnString : "");
    }

    public static string ValueContain(this string? original, string rangeString, string returnString)
    {
        return (original != null && rangeString.StartsWith(original, StringComparison.OrdinalIgnoreCase) ? returnString : "");
    }

    public static string ValueContain(this string? original, IEnumerable<string> range, string returnString)
    {
        return (original != null && range.Where(x => x.Equals(original, StringComparison.OrdinalIgnoreCase)).Count() > 0) ? returnString : "";
    }

    public static string ValueCompare(this int original, int compareValue, string returnString)
    {
        return (original == compareValue) ? returnString : "";
    }

    public static string ValueCompare(this long original, long compareValue, string returnString)
    {
        return (original == compareValue) ? returnString : "";
    }

    public static string ValueCompare(this double original, double compareValue, string returnString)
    {
        return (original == compareValue) ? returnString : "";
    }

    public static string ValueCompare(this float original, float compareValue, string returnString)
    {
        return (original == compareValue) ? returnString : "";
    }

    public static string ValueCompare(this bool original, bool compareValue, string returnString)
    {
        return (original == compareValue) ? returnString : "";
    }

    public static List<string> FindImgTags(this string html)
    {
        Regex regex = new Regex("<img.+?src=[\"'](.+?)[\"'].*?>");
        MatchCollection matches = regex.Matches(html);

        var imgTags = new List<string>();
        for (int i = 0; i < matches.Count; i++)
        {
            imgTags.Add(matches[i].Value);
        }

        return imgTags;
    }

    public static string RemoveTag(this string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", string.Empty);
    }

    public static string ReplaceImgSrc(this string html, string replacement)
    {
        string pattern = "<img\\s+[^>]*?src\\s*=\\s*['\"]([^'\"]*?)['\"][^>]*?>";
        Regex regex = new Regex(pattern);

        string replacedHtml = regex.Replace(html, match =>
        {
            string srcValue = match.Groups[1].Value;
            return $"<img src=\"{replacement}\" />";
        });

        return replacedHtml;
    }

    public static string ExtractImgSrc(this string html)
    {
        string imgSrc = string.Empty;

        string pattern = "<img\\s+[^>]*?src\\s*=\\s*['\"]([^'\"]*?)['\"][^>]*?>";
        Regex regex = new Regex(pattern);

        MatchCollection matches = regex.Matches(html);
        foreach (Match match in matches)
        {
            imgSrc = match.Groups[1].Value;
            if (!string.IsNullOrWhiteSpace(imgSrc))
            {
                break;
            }
        }

        return imgSrc;
    }

    public static string toEnter(this string content)
    {
        return content.Replace("\n", "<br/>").Replace("\r\n", "<br/>").Replace(Environment.NewLine, "<br/>").Trim();
    }

    public static List<string> GetYoutubeURLFromTags(this string content)
    {
        List<string> result = new List<string>();

        Regex rxImages = new Regex("https://www.youtube.com/[\\w]{1,}/[\\w\\-]{1,}", RegexOptions.IgnoreCase);
        MatchCollection mc = rxImages.Matches(content);
        foreach (Match m in mc)
        {
            foreach (Capture group in m.Groups)
            {
                result.Add(group.Value);
            }
        }

        return result;
    }
}