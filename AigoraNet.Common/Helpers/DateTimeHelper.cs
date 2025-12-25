namespace AigoraNet.Common.Helpers;

public static class DateTimeHelper
{
    public static DateTime FirstDay(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, 1);
    }

    public static DateTime LastDay(this DateTime dt)
    {
        return dt.FirstDay().AddMonths(1).AddDays(-1);
    }

    public static DateTime FirstTime(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
    }

    public static DateTime LastTime(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);
    }

    public static DateTime PreviousMonth(this DateTime dt)
    {
        return dt.FirstDay().AddMonths(-1);
    }

    public static DateTime NextMonth(this DateTime dt)
    {
        return dt.FirstDay().AddMonths(1);
    }

    public static DateTime PreviousYear(this DateTime dt)
    {
        return dt.FirstDay().AddYears(-1);
    }

    public static DateTime NextYear(this DateTime dt)
    {
        return dt.FirstDay().AddYears(1);
    }

    public static TimeSpan BetweenDate(this DateTime st, DateTime ed)
    {
        TimeSpan result = (ed > st) ? ed - st : st - ed;
        return result;
    }

    public static DateTime LastWeek(this DateTime st, DayOfWeek week)
    {
        if (st.DayOfWeek != week)
        {
            while (st.DayOfWeek != week)
            {
                st = st.AddDays(-1);
            }

            return st;
        }
        else
        {
            return st;
        }
    }

    public static string FormatTimeAgo(this DateTime dateTime)
    {
        TimeSpan timeDifference = DateTime.Now - dateTime;

        if (timeDifference.TotalMinutes < 1)
        {
            return "just now";
        }
        else if (timeDifference.TotalMinutes < 60)
        {
            return $"{(int)timeDifference.TotalMinutes} min ago";
        }
        else if (timeDifference.TotalHours < 24)
        {
            return $"{(int)timeDifference.TotalHours} hour ago";
        }
        else if (timeDifference.TotalDays < 30)
        {
            return $"{(int)timeDifference.TotalDays} day ago";
        }
        else
        {
            return dateTime.ToString("yyyy.MM.dd HH:mm");
        }
    }
}