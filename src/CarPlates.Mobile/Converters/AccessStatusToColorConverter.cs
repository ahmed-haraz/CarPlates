using System.Globalization;

namespace CarPlates.Mobile.Converters;

// Renamed conceptually from the old sync-status indicator: now colors a
// scan's vehicle access status (Allowed/Denied/Pending) instead of a local
// upload/sync state, since there is no offline sync queue any more.
public class AccessStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var status = value?.ToString();
        return status switch
        {
            "Allowed" => Colors.Green,
            "Pending" => Colors.Orange,
            "Denied" => Colors.Red,
            _ => Colors.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
