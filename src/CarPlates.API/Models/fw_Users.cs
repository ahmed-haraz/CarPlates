using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPlates.API.Models;

public class fw_Users
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ID { get; set; }
   
    public string? UserName { get; set; }
    
    public string? UserFullName_Ar { get; set; }
    
    public string? UserFullName_En { get; set; }
    
    public int? BranchID { get; set; }
    
    public int? StoreID { get; set; }
    
    public int? CashBoxID { get; set; }
    
    public int? TerminalID { get; set; }
    
    public string? Password { get; set; }
    
    public string? MobilePassword { get; set; }
    
    public int? DepID { get; set; }
    
    public string? DiscountPassword { get; set; }
    
    public string? Add1 { get; set; }
    
    public string? Add2 { get; set; }
    
    public string? Phone1 { get; set; }
    
    public string? Phone2 { get; set; }
    
    public string? Mobile { get; set; }
    
    public string? email { get; set; }
    
    public string? ComputerName { get; set; }
    
    public string? IPAdd { get; set; }
    
    public long? JobID { get; set; }
    
    public byte? Module { get; set; }
    
    public bool CanSelectBranchInReport { get; set; }
   
    public byte? OnLineStatus { get; set; }
    
    public int? CarID { get; set; }
    
    public int? SalesRepID { get; set; }
    
    public int? UserType { get; set; }
    
    public byte? Status { get; set; }
   
    public long? InsertUserID { get; set; }
    
    public long? UpdateUserID { get; set; }
    
    public long? InsertDateTime { get; set; }
    
    public long? UpdateDateTime { get; set; }
}
