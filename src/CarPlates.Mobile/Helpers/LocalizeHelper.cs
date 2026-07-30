using CarPlates.Mobile.Localization;

namespace CarPlates.Mobile.Helpers;

public static class LocalizeHelper
{
    public static string Localize(string? ar, string? en)
    {
        var isRtl = LocalizationResourceManager.Instance.IsRightToLeft;

        return isRtl
            ? (ar ?? en ?? string.Empty)
            : (en ?? ar ?? string.Empty);
    }
}