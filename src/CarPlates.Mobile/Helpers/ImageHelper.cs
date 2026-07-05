namespace CarPlates.Mobile.Helpers;

public static class ImageHelper
{
    public static async Task<string?> SavePhotoAsync(byte[] imageData, string fileName)
    {
        try
        {
            var photosDir = Path.Combine(FileSystem.AppDataDirectory, "photos");
            Directory.CreateDirectory(photosDir);

            var filePath = Path.Combine(photosDir, fileName);
            await File.WriteAllBytesAsync(filePath, imageData);
            return filePath;
        }
        catch
        {
            return null;
        }
    }

    public static void CleanupOldPhotos(int maxAgeDays = 30)
    {
        var photosDir = Path.Combine(FileSystem.AppDataDirectory, "photos");
        if (!Directory.Exists(photosDir)) return;

        var cutoff = DateTime.Now.AddDays(-maxAgeDays);
        foreach (var file in Directory.GetFiles(photosDir))
        {
            if (File.GetCreationTime(file) < cutoff)
            {
                try { File.Delete(file); } catch { /* ignore */ }
            }
        }
    }
}
