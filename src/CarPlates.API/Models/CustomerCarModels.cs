namespace CarPlates.API.Models;

// ---- dbo.wh_CustomerCars (writable table) ----
public class CustomerCar
{
    public long Id { get; set; }
    public int? CustomerID { get; set; }
    public int? CarMakesID { get; set; }
    public int? CarModelID { get; set; }
    public string? PlateNumber { get; set; }
    public string? VIN { get; set; }
    public int? VehicleType { get; set; }
    public int? EngineType { get; set; }
    public long? Distance { get; set; }
    public int? VehicleYear { get; set; }
    public string? Color { get; set; }
    public int? VehicleStatus { get; set; }
    public string? PlateType { get; set; }
    public int? Status { get; set; }
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
}

// ---- dbo.wh_Customers (writable table) ----
// Only the columns we actually read/write are modelled with real names; every other
// NOT NULL column on the real table has a DB-level DEFAULT constraint, so leaving the
// matching CLR property at its default (0 / false) satisfies the constraint on insert.
public class WhCustomer
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name_Ar { get; set; } = string.Empty;
    public string Name_En { get; set; } = string.Empty;
    public int? RegionID { get; set; }
    public int? CityID { get; set; }
    public string? Address { get; set; }
    public bool CustomerSupplier { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Mobile { get; set; }
    public string? email { get; set; }
    public int CreditLimit { get; set; }
    public int CreditPeriod { get; set; }
    public byte PriceID { get; set; } = 1;
    public int? DistrictID { get; set; }
    public int? WorklineId { get; set; }
    public int? SalesRepId { get; set; }
    public int? Dayid { get; set; }
    public int? GlAccId { get; set; }
    public bool Inactive { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? FileNumber { get; set; }
    public string? TaxOffice { get; set; }
    public int CustomerGroupID { get; set; }
    public int CustomerCategoryId { get; set; }
    public int StoreID { get; set; }
    public int? SupplierAcc { get; set; }
    public bool CallCenter { get; set; }
    public byte? ContractType { get; set; }
    public byte Status { get; set; } = 1;
    public int? Device_ID { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Terminal_ID { get; set; }
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
    public long CardId { get; set; }
    public bool IsTax { get; set; }
}

// ---- dbo.wh_CustomersBranch (writable table) ----
// ParentID = wh_Customers.ID this branch link belongs to. BranchID = the branch the
// customer is registered at (defaults to 1). (ParentID, BranchID) is unique.
public class CustomerBranch
{
    public int Id { get; set; }
    public int? Device_ID { get; set; }
    public string? Terminal_ID { get; set; }
    public int? ParentID { get; set; }
    public int? BranchID { get; set; }
    public byte Status { get; set; } = 1;
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
}

// ---- Lookup tables (read-only from the API's point of view) ----
public class CarMake
{
    public int MakeID { get; set; }
    public string MakeName { get; set; } = string.Empty;
    public string? IconThumbURL { get; set; }
    public string? IconOptimizedURL { get; set; }
    public string? IconOriginalURL { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CarModel
{
    public int ModelID { get; set; }
    public int MakeID { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public class VehicleTypeLookup
{
    public int Id { get; set; }
    public int? Code { get; set; }
    public string? Name_ar { get; set; }
    public string? Name_en { get; set; }
    public int? Status { get; set; }
}

public class VehicleStatusLookup
{
    public int Id { get; set; }
    public int? Code { get; set; }
    public string? Name_ar { get; set; }
    public string? Name_en { get; set; }
    public int? Status { get; set; }
}

public class EngineTypeLookup
{
    public int Id { get; set; }
    public int? Code { get; set; }
    public string? Name_ar { get; set; }
    public string? Name_en { get; set; }
    public int? Status { get; set; }
}

// ---- dbo.wh_ScanRecords (writable table backing vw_CarsPlatesDashBoard) ----
public class ScanEvent
{
    public long Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public long? CustomerCarID { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
    public string? DeviceId { get; set; }
    public int? BranchID { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }
    public byte Status { get; set; } = 1;
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
}

// ---- dbo.VW_WH_CustomerCarsFull (read-only view, see database/002_VW_WH_CustomerCarsFull.sql) ----
public class CustomerCarFull
{
    public long Id { get; set; }
    public string? PlateNumber { get; set; }
    public string? VIN { get; set; }
    public string? Color { get; set; }
    public int? VehicleYear { get; set; }
    public long? Distance { get; set; }
    public int? CarStatus { get; set; }
    public string? PlateType { get; set; }

    public int? CarMakesID { get; set; }
    public string? MakeName { get; set; }
    public int? CarModelID { get; set; }
    public string? ModelName { get; set; }

    public int? VehicleTypeID { get; set; }
    public string? VehicleTypeName_Ar { get; set; }
    public string? VehicleTypeName_En { get; set; }

    public int? VehicleStatusID { get; set; }
    public string? VehicleStatusName_Ar { get; set; }
    public string? VehicleStatusName_En { get; set; }

    public int? EngineTypeID { get; set; }
    public string? EngineTypeName_Ar { get; set; }
    public string? EngineTypeName_En { get; set; }

    public int? CustomerID { get; set; }
    public string? CustomerCode { get; set; }
    public string? CustomerName_Ar { get; set; }
    public string? CustomerName_En { get; set; }
    public string? CustomerPhone1 { get; set; }
    public string? CustomerPhone2 { get; set; }
    public string? CustomerMobile { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerAddress { get; set; }
    public int? CustomerCityID { get; set; }
    public int? CustomerRegionID { get; set; }
    public bool? CustomerInactive { get; set; }
    public int? CustomerStoreID { get; set; }

    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
}
