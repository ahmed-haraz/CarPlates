namespace CarPlates.API.Models;

public class Vehicle
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string PlateType { get; set; } = string.Empty; // Egyptian, English, etc.
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? OwnerNationalId { get; set; }
    public string AccessStatus { get; set; } = "Allowed"; // Allowed, Denied, Pending
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    // Navigation property
    public ICollection<ScanRecord> ScanRecords { get; set; } = new List<ScanRecord>();
}
