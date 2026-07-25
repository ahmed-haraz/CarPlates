namespace CarPlates.Mobile.Platforms.Android;

public class BluetoothConnectPermission : Permissions.BasePlatformPermission
{
    private static readonly string[] Android12Permissions =
        new[] { global::Android.Manifest.Permission.BluetoothConnect, global::Android.Manifest.Permission.BluetoothScan };

    private static readonly string[] LegacyPermissions =
        new[] { global::Android.Manifest.Permission.Bluetooth, global::Android.Manifest.Permission.BluetoothAdmin };

    public override (string androidPermission, bool isRuntime)[] RequiredPermissions
    {
        get
        {
            var perms = OperatingSystem.IsAndroidVersionAtLeast(31) ? Android12Permissions : LegacyPermissions;
            return perms.Select(p => (p, false)).ToArray();
        }
    }

    public override Task<PermissionStatus> CheckStatusAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return Task.FromResult(PermissionStatus.Granted);
        return base.CheckStatusAsync();
    }
}
