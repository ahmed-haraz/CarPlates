using System.Globalization;

namespace CarPlates.Mobile.Converters;

public class ConfidenceToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float confidence)
        {
            return confidence switch
            {
                >= 0.9f => Colors.Green,
                >= 0.75f => Colors.Orange,
                >= 0.5f => Colors.Yellow,
                _ => Colors.Red
            };
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
