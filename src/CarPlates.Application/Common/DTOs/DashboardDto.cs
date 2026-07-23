namespace CarPlates.Application.Common.DTOs;

public record DashboardStatisticsDto(
    int TotalScans,
    int TodayScans,
    int TotalVehicles,
    int AllowedVehicles,
    int DeniedVehicles,
    int PendingVehicles,
    int TotalRegisteredCars,
    int TotalCustomers,
    int TodayBills = 0,
    double TodaySalesTotal = 0);

public record RecentScanDto(
    int Id,
    string PlateNumber,
    string? VehicleBrand,
    string? AccessStatus,
    DateTime ScanTime);
