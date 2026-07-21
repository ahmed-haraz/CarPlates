namespace CarPlates.Mobile.Helpers;

public static class IconHelper
{
    public static readonly BindableProperty GlyphProperty =
        BindableProperty.CreateAttached(
            "Glyph",
            typeof(string),
            typeof(IconHelper),
            default(string),
            propertyChanged: OnIconChanged);

    public static readonly BindableProperty LightColorResourceKeyProperty =
        BindableProperty.CreateAttached(
            "LightColorResourceKey",
            typeof(string),
            typeof(IconHelper),
            "IconLight");

    public static readonly BindableProperty DarkColorResourceKeyProperty =
        BindableProperty.CreateAttached(
            "DarkColorResourceKey",
            typeof(string),
            typeof(IconHelper),
            "IconDark");

    public static string GetGlyph(BindableObject view)
        => (string)view.GetValue(GlyphProperty);

    public static void SetGlyph(BindableObject view, string value)
        => view.SetValue(GlyphProperty, value);

    public static string GetLightColorResourceKey(BindableObject view)
        => (string)view.GetValue(LightColorResourceKeyProperty);

    public static void SetLightColorResourceKey(BindableObject view, string value)
        => view.SetValue(LightColorResourceKeyProperty, value);

    public static string GetDarkColorResourceKey(BindableObject view)
        => (string)view.GetValue(DarkColorResourceKeyProperty);

    public static void SetDarkColorResourceKey(BindableObject view, string value)
        => view.SetValue(DarkColorResourceKeyProperty, value);

    private static void OnIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Image image || newValue is not string glyph)
            return;

        var source = new FontImageSource
        {
            Glyph = glyph,
            FontFamily = "IcoMoon"
        };

        var lightColor = ResolveColor(GetLightColorResourceKey(image), Colors.Black);
        var darkColor = ResolveColor(GetDarkColorResourceKey(image), Colors.White);

        source.SetAppThemeColor(FontImageSource.ColorProperty, lightColor, darkColor);
        image.Source = source;
    }

    private static Color ResolveColor(string? resourceKey, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(resourceKey)
            && Application.Current?.Resources.TryGetValue(resourceKey, out var resource) == true
            && resource is Color color)
        {
            return color;
        }

        return fallback;
    }
}
