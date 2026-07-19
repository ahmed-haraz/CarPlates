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

    public static string GetGlyph(BindableObject view)
        => (string)view.GetValue(GlyphProperty);

    public static void SetGlyph(BindableObject view, string value)
        => view.SetValue(GlyphProperty, value);

    private static void OnIconChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Image image || newValue is not string glyph)
            return;

        var source = new FontImageSource
        {
            Glyph = glyph,
            FontFamily = "IcoMoon"
        };

        // ✅ SAFE theme-aware colors
        var resources = Microsoft.Maui.Controls.Application.Current?.Resources;

        if (resources != null
            && resources.TryGetValue("Primary", out var light)
            && resources.TryGetValue("White", out var dark)
            && light is Color lightColor
            && dark is Color darkColor)
        {
            source.SetAppThemeColor(
                FontImageSource.ColorProperty,
                lightColor,
                darkColor
            );
        }
        else
        {
            // 🔁 Fallback (never invisible)
            source.Color = Colors.Red;
        }

        image.Source = source;
    }
}

