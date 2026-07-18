namespace CarPlates.Application.Common.DTOs;

public record DashboardStatisticsDto(
    int TotalScans,
    int TodayScans,
    int TotalVehicles,
    int AllowedVehicles,
    int DeniedVehicles,
    int PendingVehicles,
    int TotalRegisteredCars,
    int TotalCustomers);

public record RecentScanDto(
    Guid Id,
    string PlateNumber,
    string? VehicleBrand,
    string? AccessStatus,
    DateTime ScanTime);
