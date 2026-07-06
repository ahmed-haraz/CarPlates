namespace CarPlates.API.Models;

public class ScanRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
    public string? ScannedByUserId { get; set; }
    public string? DeviceId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vehicle Vehicle { get; set; } = null!;
}
