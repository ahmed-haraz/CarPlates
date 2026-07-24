namespace CarPlates.API.Models;

public class WhPrTrans
{
    public long ID { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public int TransDate { get; set; }
    public int BranchID { get; set; }
    public long? glEntryHeaderId { get; set; }
    public int PrTransType { get; set; }
    public double TransVal { get; set; }
    public string? TransNotes { get; set; }
    public long? InvHeaderID { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int? SalesRepId { get; set; }
    public int? SupervisorId { get; set; }
    public int? CashBoxNo { get; set; }
    public byte? PaymentType { get; set; }
    public byte? PayType { get; set; }
    public long? CheckNo { get; set; }
    public int? BankId { get; set; }
    public int? IssueDate { get; set; }
    public int? DueDate { get; set; }
    public byte? CheckStatus { get; set; }
    public int? StoreId { get; set; }
    public int? ChkHdrid { get; set; }
    public byte? CHECKTYPE { get; set; }
    public byte? MyBank { get; set; }
    public int? MyBankId { get; set; }
    public bool GlPosted { get; set; }
    public bool CloseStatus { get; set; }
    public string? DocTransNo { get; set; }
    public int CurrencyId { get; set; }
    public double CurrencyRate { get; set; }
    public double TotalCurrency { get; set; }
    public int? AccountID { get; set; }
    public int? NewCustomerId { get; set; }
    public int? NewSupplierId { get; set; }
    public int? CostCenterID { get; set; }
    public bool? PrevYear { get; set; }
    public long? DeviceHdrID { get; set; }
    public string? Terminal_ID { get; set; }
    public double JWQtyIn21 { get; set; }
    public double JWQtyIn18 { get; set; }
    public int? TransSourceType { get; set; }
    public long? TransNo { get; set; }
    public byte? SourceType { get; set; }
    public bool? Has_InvDetails { get; set; }
    public bool? b_OpeningBal { get; set; }
    public long? MobileCustomerID { get; set; }
    public byte Status { get; set; }
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }
    public int? CompanyID { get; set; }
    public long? MobileHeaderID { get; set; }
    public double? Serial { get; set; }
    public string? TransNotes_AR { get; set; }
    public string? TransNotes_EN { get; set; }
}
