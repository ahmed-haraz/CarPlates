namespace CarPlates.API.Models;

public class ScanRecord
{
    public int Id { get; set; } 
    public string PlateNumber { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public DateTime? ScanTime { get; set; } = DateTime.UtcNow;
    public string? ScannedByUserId { get; set; }
    public string? DeviceId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? Notes { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte IsDeleted { get; set; } = 0;
}
