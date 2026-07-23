using System.Globalization;
using System.Text;

namespace CarPlates.Mobile.Converters;

/// <summary>
/// Converts an Arabic plate number string (e.g. "ب س ق ١٢٣٤")
/// to its English-transliterated equivalent (e.g. "B S Q 1234").
/// </summary>
public class ArabicToEnglishPlateConverter : IValueConverter
{
    private static readonly Dictionary<char, char> ArabicToEnglish = new()
    {
        ['ا'] = 'A',
        ['أ'] = 'A',
        ['إ'] = 'E',
        ['آ'] = 'A',
        ['ب'] = 'B',
        ['ت'] = 'T',
        ['ث'] = 'S',
        ['ج'] = 'J',
        ['ح'] = 'H',
        ['خ'] = 'X',
        ['د'] = 'D',
        ['ذ'] = 'Z',
        ['ر'] = 'R',
        ['ز'] = 'Z',
        ['س'] = 'S',
        ['ش'] = 'C',
        ['ص'] = 'S',
        ['ض'] = 'D',
        ['ط'] = 'T',
        ['ظ'] = 'Z',
        ['ع'] = 'A',
        ['غ'] = 'G',
        ['ف'] = 'F',
        ['ق'] = 'Q',
        ['ك'] = 'K',
        ['ل'] = 'L',
        ['م'] = 'M',
        ['ن'] = 'N',
        ['ه'] = 'H',
        ['و'] = 'W',
        ['ي'] = 'Y',
        ['ة'] = 'H',
        ['ى'] = 'A',
        ['ء'] = 'A',
        ['ئ'] = 'Y',
        ['ؤ'] = 'W',
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string plate || string.IsNullOrWhiteSpace(plate))
            return value;

        var sb = new StringBuilder(plate.Length);
        foreach (var c in plate)
        {
            if (ArabicToEnglish.TryGetValue(c, out var eng))
                sb.Append(eng);
            else if (char.IsDigit(c) || c == '-' || c == '_')
                sb.Append(c);
            else if (c <= 127)
                sb.Append(char.ToUpperInvariant(c));
            else if (c == ' ')
                sb.Append(' ');
        }
        return sb.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}
