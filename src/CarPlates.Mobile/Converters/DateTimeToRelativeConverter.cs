using System.Globalization;

namespace CarPlates.Mobile.Converters;

public class DateTimeToRelativeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var diff = DateTime.Now - dateTime;
            return diff.TotalMinutes < 1 ? "Just now" :
                   diff.TotalHours < 1 ? $"{diff.Minutes}m ago" :
                   diff.TotalDays < 1 ? $"{diff.Hours}h ago" :
                   diff.TotalDays < 7 ? $"{diff.Days}d ago" :
                   dateTime.ToString("MMM dd, yyyy");
        }
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
