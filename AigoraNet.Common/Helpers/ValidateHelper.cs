using System.Text.RegularExpressions;

namespace AigoraNet.Common.Helpers;

public class ValidateHelper
{
    public static bool IsValidPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        // 비밀번호 길이 검사
        if (password.Length < 8)
            return false;

        // 각각의 조건을 만족하는지 확인
        bool hasLetter = Regex.IsMatch(password, @"[A-Za-z]");
        bool hasDigit = Regex.IsMatch(password, @"\d");
        bool hasSpecialChar = Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>_\-\\\/[\]=+~`'`;:]");

        return hasLetter && hasDigit && hasSpecialChar;
    }


}