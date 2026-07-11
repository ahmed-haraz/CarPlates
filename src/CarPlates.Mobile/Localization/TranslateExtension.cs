using System.Globalization;
using Microsoft.Maui.Controls.Xaml;

namespace CarPlates.Mobile.Localization;

/// <summary>
/// XAML usage: Text="{loc:Translate Key=WelcomeBack}"
/// Binds to LocalizationResourceManager.Instance[Key], which re-raises PropertyChanged
/// for every key whenever the language switches, so bound text updates immediately.
/// </summary>
[ContentProperty(nameof(Key))]
[AcceptEmptyServiceProvider]
public class TranslateExtension : IMarkupExtension<BindingBase>
{
    public string Key { get; set; } = string.Empty;

    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding(
            path: $"[{Key}]",
            mode: BindingMode.OneWay,
            source: LocalizationResourceManager.Instance);
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
