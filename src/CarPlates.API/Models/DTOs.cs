namespace CarPlates.API.Models;

// Auth DTOs
public record LoginRequestDto(string Username, string Password);
public record LoginResponseDto(string AccessToken, string RefreshToken, UserDto User);
public record RefreshTokenRequestDto(string RefreshToken);
public record LogoutRequestDto(string RefreshToken);
public record UserDto(string Id, string Username, string Email, string FullName, int BranchId, int CashboxID, int CarId, int StoreId, int SalesRepID, int Usertype);
public record RegisterRequestDto(string Username, string Email, string Password, string FullName);

// Vehicle DTOs
public record VehicleDto(
    int Id,
    string PlateNumber,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName);

public record VehicleCreateDto(
    string PlateNumber,
    int BranchID,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? OwnerPhone);

public record VehicleUpdateDto(
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName,
    string? OwnerPhone,
    string? Notes);

// Scan Record DTOs
public record ScanRecordDto(
    long Id,
    string PlateNumber,
    string? PhotoUrl,
    DateTime? ScanTime,
    string? Brand,
    string? Model,
    string? Color,
    string? OwnerName);

// Scanning a plate only ever writes a wh_ScanRecords row (see ScanRecordService.CreateAsync).
// It never creates/updates wh_Customers, wh_CustomersBranch, or wh_CustomerCars - registering
// a car is a separate, deliberate action via CustomerCarsController's "scan"+RegisterAsync path.
public record ScanRecordCreateDto(
    string PlateNumber,
    string? PhotoUrl,
    string? DeviceId,
    double? Latitude,
    double? Longitude,
    int BranchID);

// Dashboard/Stats DTOs
public record DashboardStatisticsDto(
    int TotalScans,
    int TodayScans,
    int TotalVehicles,
    int TotalRegisteredCars,
    int TotalCustomers);

public record RecentScanDto(
    long Id,
    string PlateNumber,
    string? VehicleBrand,
    DateTime? ScanTime);

// Sync DTOs
public record SyncBatchRequestDto(List<ScanRecordSyncDto> Records);

public record OwnerDto(

    int ID,
    string Name_ar,
    string Name_En,
    string Phone1,
    string Phone2,
    string Mobile,
    string email,
    string Address


    );


public record ScanRecordSyncDto(
    string PlateNumber,
    DateTime? ScanTime,
    string? PhotoUrl);


public record SyncBatchResponseDto(int SyncedCount, int FailedCount, List<string> Errors);

// ---- Customer Cars (wh_CustomerCars / wh_Customers / wh_CustomersBranch) ----

public record CarMakeDto(int MakeID, string MakeName);

public record CarModelDto(int ModelID, int MakeID, string ModelName);

public record CustomerCarLookupDto(
    long Id,
    string? PlateNumber,
    string? VIN,
    string? Color,
    int? VehicleYear,
    int? CarMakesID,
    string? MakeName,
    int? CarModelID,
    string? ModelName,
    int? VehicleTypeID,
    string? VehicleTypeName_En,
    string? VehicleTypeName_Ar,
    int? VehicleStatusID,
    string? VehicleStatusName_En,
    string? VehicleStatusName_Ar,
    int? EngineTypeID,
    string? EngineTypeName_En,
    string? EngineTypeName_Ar,
    int? CustomerID,
    string? CustomerCode,
    string? CustomerName_Ar,
    string? CustomerName_En,
    string? CustomerPhone1,
    string? CustomerMobile,
    string? CustomerEmail);

// Sent by the mobile app after a plate scan. PlateNumber is required; everything else
// is only used if the plate isn't registered yet and a new car/customer must be created.
public record CustomerCarScanDto(
    string PlateNumber,
    int BranchID,
    string? VIN,
    string? Color,
    int? VehicleYear,
    int? CarMakesID,
    int? CarModelID,
    int? VehicleType,
    int? EngineType,
    string? CustomerName_Ar,
    string? CustomerName_En,
    string? CustomerMobile,
    string? CustomerPhone1);

public record CustomerCarScanResultDto(
    CustomerCarLookupDto? Car,
    bool WasNewCar,
    bool WasNewCustomer,
    bool WasNewBranchLink);

// ---- Workshop lookups (wh_CarsTechnicians / wh_Cars_WorkLocations) ----

public record TechnicianDto(int Id, int? Code, string? Name_Ar, string? Name_En);

public record WorkLocationDto(int Id, int? Code, string? Name_Ar, string? Name_En);

// ---- Categories (vw_wh_ItemSubGroups) ----

public record CategoryDto(
    int Id,
    int? Code,
    string? Name_Ar,
    string? Name_En,
    string? GroupName,
    int? ParentID,
    int? BranchID,
    string? Image);

// ---- Customers (wh_Customers), for search/lookup rather than the full registration flow ----

public record CustomerDto(
    int Id,
    string Code,
    string Name_Ar,
    string Name_En,
    string? Mobile,
    string? Phone1,
    string? Email,
    string? Address);

// ---- Vehicle attribute lookups (wh_VehicleType / wh_CarsEngineType) ----

public record VehicleTypeDto(int Id, int? Code, string? Name_Ar, string? Name_En);

public record EngineTypeDto(int Id, int? Code, string? Name_Ar, string? Name_En);

// ---- Items (vw_wh_ItemBarCodes) ----

public record ItemBarCodeDto(
    int Id,
    string? Code,
    string? Name_Ar,
    string? Name_En,
    string ItemBarCode,
    byte? Package,
    string? PackageName,
    double? PackagePrice,
    int? ItemGroupId,
    string? ItemGroupName_Ar,
    string? ItemGroupName_En,
    double? ItemTax,
    byte? Status,
    bool OpenSale = false,
    string? ItemDiscount1 = null,
    string? ItemDiscount2 = null,
    string? ItemDiscount3 = null);

// ---- Bills (wh_TransHeader / wh_TransDetails) ----

public record CreateBillDetailDto(
    string ItemBarCode,
    long ItemID,
    int? Package,
    double Qty,
    double Price,
    double? DetailDiscount1 = null,
    double? DetailTax = null,
    string? DetailNotes = null);

public record CreateBillDto(
    int? BranchID,
    int? CustomerId,
    int? EngineerId,
    int? CarHeaderId,
    int? StoreId,
    byte? PayType,
    string? Notes,
    IReadOnlyList<CreateBillDetailDto> Details);

public record BillDetailDto(
    long DetailId,
    long ItemID,
    string ItemBarCode,
    int? Package,
    double Qty,
    double Price,
    double? DetailDiscount1,
    double? DetailTax,
    double? Value);

public record BillDto(
    long HeaderId,
    string? DocTransNo,
    int? BranchID,
    int? CustomerId,
    int? EngineerId,
    int? CarHeaderId,
    double Total,
    double NetTotal,
    double Paid,
    double Balance,
    byte? PayType,
    string? Notes,
    IReadOnlyList<BillDetailDto> Details);
