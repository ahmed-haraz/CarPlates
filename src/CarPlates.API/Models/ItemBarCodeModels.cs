namespace CarPlates.API.Models;

// ---- dbo.vw_wh_ItemBarCodes (read-only reporting view, see SQLQuery3.sql) ----
// This view joins wh_Items/wh_ItemBarCodes/wh_ItemGroups/wh_ItemSubGroups/wh_Suppliers and
// exposes ~90 columns. Only the subset a bill line actually needs (identify the item by
// barcode, show its name/price/group/tax) is mapped here. It has no natural single-column
// key (a barcode can have more than one package row), so it's mapped keyless - fine for the
// read-only lookup/search use it's for. Add more columns later if a screen needs them.
public class ItemBarCodeView
{
    public int ID { get; set; }
    public string? Code { get; set; }
    public int? BranchID { get; set; }
    public string? Name_Ar { get; set; }
    public string? Name_En { get; set; }
    public string ItemBarCode { get; set; } = string.Empty;
    public byte? Package { get; set; }
    public string? PackageName { get; set; }
    public double? PackagePrice { get; set; }
    public double? PackagePurchasePrice { get; set; }
    public int? ItemGroupId { get; set; }
    public string? ItemGroupName_AR { get; set; }
    public string? ItemGroupName_En { get; set; }
    public int? ItemSubGroupId { get; set; }
    public string? ItemSubGroupName_AR { get; set; }
    public string? ItemSubGroupName_EN { get; set; }
    public byte? ItemType { get; set; }
    public double? ItemTax { get; set; }
    public double? WarrantyTax { get; set; }
    public double? TableTax { get; set; }
    public bool? OpenSale { get; set; }
    public string? ItemDiscount1 { get; set; }
    public string? ItemDiscount2 { get; set; }
    public string? ItemDiscount3 { get; set; }
    public byte? Status { get; set; }
}
