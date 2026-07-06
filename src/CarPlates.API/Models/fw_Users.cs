namespace CarPlates.API.Models;

public class fw_Users
{
    public int ID { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserFullName_Ar { get; set; } = string.Empty;
    public string UserFullName_En { get; set; } = string.Empty;
    public int BranchID { get; set; }
    public int StoreID { get; set; }
    public int CashBoxID { get; set; }
    public int TerminalID { get; set; }
    public string Password { get; set; } = string.Empty;
    public string MobilePassword { get; set; } = string.Empty;
    public int DepID { get; set; }
    public string DiscountPassword { get; set; } = string.Empty;
    public string Add1 { get; set; } = string.Empty;
    public string Add2 { get; set; } = string.Empty;
    public string Phone1 { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string IPAdd { get; set; } = string.Empty;
    public int JobID { get; set; }
    public int Module { get; set; }
    public bool CanSelectBranchInReport { get; set; }
    public int OnLineStatus { get; set; }
    public int CarID { get; set; }
    public int SalesRepID { get; set; }
    public int UserType { get; set; }
    public int FilterByUserID { get; set; }
    public int Status { get; set; }
    public int InsertUserID { get; set; }
    public int UpdateUserID { get; set; }
    public int InsertDateTime { get; set; }
    public int UpdateDateTime { get; set; }
}
