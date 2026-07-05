using System.Globalization;
using CarPlates.Shared.Extensions;

namespace CarPlates.Mobile.Converters;

public class PlateFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string plate)
        {
            // Format Egyptian plates: 1234 - ABC
            var normalized = plate.NormalizePlate();
            if (normalized.Length >= 5)
            {
                var numbers = new string(normalized.TakeWhile(char.IsDigit).ToArray());
                var letters = new string(normalized.SkipWhile(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(numbers) && !string.IsNullOrEmpty(letters))
                    return $"{numbers} - {letters}";
            }
            return plate;
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}
