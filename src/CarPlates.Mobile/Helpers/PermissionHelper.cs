namespace CarPlates.Mobile.Helpers;

public static class PermissionHelper
{
    public static async Task<bool> RequestCameraPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }
        return status == PermissionStatus.Granted;
    }

    public static async Task<bool> RequestStoragePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.StorageRead>();
        }
        return status == PermissionStatus.Granted;
    }

    public static async Task<bool> RequestAllPermissionsAsync()
    {
        var camera = await RequestCameraPermissionAsync();
        var storage = await RequestStoragePermissionAsync();
        return camera && storage;
    }
}
