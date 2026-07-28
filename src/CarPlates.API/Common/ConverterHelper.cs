using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CarPlates.API.Common;

public partial class ConverterHelper
{


    public static long ToLong(object str)
    {
        long num;
        if (str == null)
        {
            num = 0;
        }
        else if (!string.IsNullOrEmpty(str.ToString()))
        {
            if (str.ToString()!.Contains('.')) str = str.ToString()!.Split(['.'])[0];
            long.TryParse(str.ToString(), out long i);
            num = i;
        }
        else
        {
            num = 0;
        }

        return num;
    }

    public static decimal ToDecimal(object str, int iRound = 2)
    {
        if (str == null)
            return 0;

        var s = str.ToString()?.Trim();
        if (string.IsNullOrEmpty(s))
            return 0;

        // Handle input that starts with "." (like ".5") → normalize to "0.5"
        if (s.StartsWith('.'))
            s = "0" + s;

        // Handle input like "0." → keep it as "0" temporarily (prevents crash)
        if (s.EndsWith('.'))
            s = s.TrimEnd('.');

        if (decimal.TryParse(s, out var result))
            return decimal.Round(result, iRound);

        return 0;
    }

    public static string NormalizeInput(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "0";
        if (s.StartsWith('.')) return "0" + s;
        return s;
    }

    public static double ToDouble(object str)
    {
        double num;
        if (str == null)
        {
            num = 0;
        }
        else if (!string.IsNullOrEmpty(str.ToString()))
        {
            double.TryParse(str.ToString(), out double i);
            num = i;
        }
        else
        {
            num = 0;
        }

        return num;
    }

    public static double ToDouble(object value, int decimals = 6)
    {
        if (value == null)
            return 0d;

        var str = value.ToString();

        if (string.IsNullOrWhiteSpace(str))
            return 0d;

        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return Math.Round(result, decimals, MidpointRounding.AwayFromZero);
        }

        return 0d;
    }

    public static float ToFloat(object str)
    {
        float single;
        if (str == null)
        {
            single = 0f;
        }
        else if (!string.IsNullOrEmpty(str.ToString()))
        {
            float.TryParse(str.ToString(), out float i);
            single = i;
        }
        else
        {
            single = 0f;
        }

        return single;
    }

    public static int ToInt(object? value)
    {
        if (value == null)
            return 0;

        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        text = text.Trim()
                   .Replace("\"", "")
                   .Replace("\\n", "");

        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            return 1;

        if (text.Contains('.'))
            text = text.Split('.')[0];

        return int.TryParse(text, out var result) ? result : 0;
    }

    public static string ToString(object str)
    {
        string str1;
        str1 = str != null ? str.ToString()! : string.Empty;
        return str1!;
    }

    public static bool ToBoolean(object? value)
    {
        if (value == null)
            return false;

        var text = value.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();

        return text switch
        {
            "true" => true,
            "false" => false,
            "1" => true,
            "0" => false,
            _ => bool.TryParse(text, out var result) && result
        };
    }


    public static string GetDate(bool slash)
    {
        var now = DateTime.Now;

        var year = now.Year.ToString();
        var month = now.Month.ToString("D2");
        var day = now.Day.ToString("D2");

        var date = slash ? $"{year}/{month}/{day}" : $"{year}{month}{day}";
        return date;
    }

    public static int GetDate()
    {
        var now = DateTime.Now;
        var year = now.Year.ToString();
        var month = now.Month.ToString("D2");
        var day = now.Day.ToString("D2");
        var date = $"{year}{month}{day}";
        return ToInt(date);
    }

    public static long GetDateTime()
    {
        var now = DateTime.Now;
        var year = now.Year.ToString();
        var month = now.Month.ToString("D2");
        var day = now.Day.ToString("D2");
        var hour = now.Hour.ToString("D2");
        var minute = now.Minute.ToString("D2");
        var second = now.Second.ToString("D2");
        return ToLong($"{year}{month}{day}{hour}{minute}{second}");
    }

    public static TimeSpan GetTimeSpan(string hour, string minute)
    {
        return new TimeSpan(0, ToInt(hour), ToInt(minute), 0);
    }

    public static string ConvertLongToDateTime(long longFormat)
    {
        // Parse the input string to DateTime
        if (DateTime.TryParseExact($"{longFormat}", "yyyyMMddHHmmss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            // Format as 12-hour time with AM/PM
            return date.ToString("yyyy-MM-dd hh:mm:ss tt", CultureInfo.InvariantCulture);
        }

        // Return original or error message if parsing fails
        return "Invalid date format";
    }

    public static string ConvertIntToDateString(int intDate)
    {
        // Ensure the int is 8 digits, e.g. 20250616
        var dateString = intDate.ToString("D8");

        // Try to parse the date string
        if (DateTime.TryParseExact(dateString, "yyyyMMdd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result.ToString("yyyy-MM-dd");
        }

        // Return empty string on failure (or a default message if you prefer)
        return string.Empty;
    }

    public static DateTime ConvertIntToDate(int intDate)
    {
        // Ensure the int is 8 digits (e.g. 20250915)
        var dateString = intDate.ToString("D8");

        if (DateTime.TryParseExact(
                dateString,
                "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var result))
        {
            return result; // return valid DateTime
        }

        // Return null if parsing fails
        return DateTime.Now;
    }

    public static int ConvertDateToInt(DateTime visitDate)
    {
        return ToInt(visitDate.ToString("yyyyMMdd"));
    }

    public static T? Deserialize<T>(Dictionary<string, string> dict, string key) where T : class
    {
        return dict.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? JsonConvert.DeserializeObject<T>(value)
            : null;
    }

    public static int GetInt(Dictionary<string, string> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? ToInt(value) : 0;
    }

    public static bool GetBool(Dictionary<string, string> dict, string key)
    {
        return dict.TryGetValue(key, out var value) && ToBoolean(value);
    }

    public static string GetString(Dictionary<string, string> dict, string key) =>
    dict.TryGetValue(key, out var val) ? val : string.Empty;

    public static bool IsWithin(decimal value, decimal minimum, decimal maximum)
    {
        return value >= minimum && value <= maximum;
    }

    public static string RemoveDateSlash(string date)
    {
        var pattern = @"[/\-]";
        var datet = Regex.Replace(date, pattern, "");
        return datet;
    }

    public static string FormatTimestamp(long timestamp, bool includeDate = true, bool includeTime = true)
    {
        string ts = timestamp.ToString();

        if (!DateTime.TryParseExact(ts, "yyyyMMddHHmmss", null, DateTimeStyles.None, out DateTime dateTime))
            throw new ArgumentException("Invalid timestamp format");

        string datePart = includeDate ? dateTime.ToString("yyyy-MM-dd") : string.Empty;
        string timePart = includeTime ? dateTime.ToString("hh:mm tt") : string.Empty;

        if (includeDate && includeTime)
            return $"{datePart} {timePart}";
        else if (includeDate)
            return datePart;
        else if (includeTime)
            return timePart;

        return string.Empty;
    }
}