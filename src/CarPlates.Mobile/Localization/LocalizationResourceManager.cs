using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace CarPlates.Mobile.Localization;

/// <summary>
/// Central place that owns "what language is the app in right now" and lets both
/// XAML (via the indexer + PropertyChanged) and C# code (via <see cref="AppResources"/>)
/// read translated strings that update immediately when the language changes -
/// no app restart required.
/// </summary>
public class LocalizationResourceManager : INotifyPropertyChanged
{
    private const string ResourceBaseName = "CarPlates.Mobile.Resources.Strings.AppResources";

    private static readonly Lazy<LocalizationResourceManager> LazyInstance = new(() => new LocalizationResourceManager());
    public static LocalizationResourceManager Instance => LazyInstance.Value;

    private readonly ResourceManager _resourceManager;

    private LocalizationResourceManager()
    {
        _resourceManager = new ResourceManager(ResourceBaseName, typeof(LocalizationResourceManager).Assembly);
    }

    public CultureInfo CurrentCulture { get; private set; } = CultureInfo.CurrentUICulture;

    public bool IsRightToLeft => CurrentCulture.TextInfo.IsRightToLeft;

    public FlowDirection FlowDirection => IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    /// <summary>Looks up a string for the given key in the current culture. Falls back to
    /// "#key#" (visibly broken but non-crashing) if a translation is missing, so a gap in
    /// the Arabic resx never brings down a page - it just shows something obviously wrong
    /// that's easy to spot and translate later.</summary>
    public string this[string key] => _resourceManager.GetString(key, CurrentCulture) ?? $"#{key}#";

    /// <summary>Switches the app's language at runtime. Call from Settings (and once at
    /// startup with the persisted preference) - both AppResources/XAML bindings and
    /// FlowDirection react to this.</summary>
    public void SetCulture(CultureInfo culture)
    {
        if (Equals(CurrentCulture, culture)) return;

        CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        // Empty property name is the INotifyPropertyChanged convention for "everything
        // changed" - every {loc:Translate Key=X} binding (bound to this[X]) re-evaluates.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRightToLeft)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
