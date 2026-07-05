namespace CarPlates.Shared.Extensions;

public static class StringExtensions
{
    public static string ToArabicNumbers(this string input)
    {
        return input
            .Replace('0', '٠').Replace('1', '١').Replace('2', '٢')
            .Replace('3', '٣').Replace('4', '٤').Replace('5', '٥')
            .Replace('6', '٦').Replace('7', '٧').Replace('8', '٨')
            .Replace('9', '٩');
    }

    public static string ToEnglishNumbers(this string input)
    {
        return input
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2')
            .Replace('٣', '3').Replace('٤', '4').Replace('٥', '5')
            .Replace('٦', '6').Replace('٧', '7').Replace('٨', '8')
            .Replace('٩', '9');
    }

    public static string NormalizePlate(this string input)
    {
        return input.Trim().ToUpperInvariant()
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("_", "");
    }

    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);
}
