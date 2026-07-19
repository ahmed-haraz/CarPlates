namespace CarPlates.API.Models;

// ---- dbo.wh_CarsTechnicians (view over wh_CarsTechnician, see SQLQuery3.sql) ----
public class CarsTechnician
{
    public int Id { get; set; }
    public int? Code { get; set; }
    public string? Name_ar { get; set; }
    public string? Name_en { get; set; }
    public byte Status { get; set; }
    public long? InsertUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateUserID { get; set; }
    public long? UpdateDateTime { get; set; }
}

// ---- dbo.wh_Cars_WorkLocations (view over wh_WorkLocations, see SQLQuery3.sql) ----
public class WorkLocation
{
    public int Id { get; set; }
    public int? Code { get; set; }
    public string? Name_ar { get; set; }
    public string? Name_en { get; set; }
    public byte Status { get; set; }
    public long? InsertUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateUserID { get; set; }
    public long? UpdateDateTime { get; set; }
}
