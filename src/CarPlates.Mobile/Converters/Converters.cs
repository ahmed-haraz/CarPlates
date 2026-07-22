using System.Globalization;

namespace CarPlates.Mobile.Converters;

public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value != null;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is int count && count > 0;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ObjectToListConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return new List<object>();
        return new List<object> { value };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? parameter : null;
}

public class ColorNameToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var colorName = value?.ToString() ?? "Gray";
        return colorName.ToLower() switch
        {
            "beige" => Colors.Beige,
            "black" => Colors.Black,
            "blue" => Colors.Blue,
            "bronze" => Colors.Brown,
            "brown" => Colors.Brown,
            "gold" => Colors.Gold,
            "gray" => Colors.Gray,
            "green" => Colors.Green,
            "orange" => Colors.Orange,
            "pink" => Colors.Pink,
            "purple" => Colors.Purple,
            "red" => Colors.Red,
            "silver" => Colors.Silver,
            "white" => Colors.White,
            "yellow" => Colors.Yellow,
            _ => Colors.Gray
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FilterToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var filter = value?.ToString();
        var selected = parameter?.ToString();
        return filter == selected ? Color.FromArgb("#8BC34A") : Colors.White;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FilterToTextColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var filter = value?.ToString();
        var selected = parameter?.ToString();
        return filter == selected ? Colors.White : Colors.Black;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "غير معين" => Colors.Gray,
            "تم التعيين" => Colors.Orange,
            "قيد الخدمة" => Colors.Blue,
            "ملغاة" => Colors.Red,
            _ => Colors.Gray
        };
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IsStringEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string str && string.IsNullOrEmpty(str);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BrandToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var brand = value?.ToString();
        if (string.IsNullOrWhiteSpace(brand))
            return "car.svg";
        return $"brand_{brand.Replace(" ", "_").ToLower()}.png";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
