using System.Globalization;

namespace CarPlates.Mobile.Converters;

public class SyncStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value?.ToString();
        return status switch
        {
            "Synced" => Colors.Green,
            "Pending" => Colors.Orange,
            "Failed" => Colors.Red,
            "Offline" => Colors.Gray,
            _ => Colors.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
