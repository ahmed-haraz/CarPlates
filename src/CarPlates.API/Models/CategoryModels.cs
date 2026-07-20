namespace CarPlates.API.Models;

// ---- dbo.vw_wh_ItemSubGroups (read-only view, see SQL provided by the user) ----
// Joins wh_ItemSubGroups to wh_ItemGroups (for the parent group's name) and inner-joins
// wh_ItemSubGroupsbranch, so a subgroup shows up once per branch it's active in - no
// single-column key, so this is mapped keyless like the item barcode view.
public class ItemSubGroupView
{
    public int ID { get; set; }
    public int? Code { get; set; }
    public string? Name_AR { get; set; }
    public string? Name_En { get; set; }
    public string? Groupname { get; set; }
    public byte Status { get; set; }
    public int? ParentID { get; set; }
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
    public int? BranchID { get; set; }
    public string? Image { get; set; }
}
