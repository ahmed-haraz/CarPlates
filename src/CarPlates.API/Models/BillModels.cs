namespace CarPlates.API.Models;

// ---- dbo.wh_TransHeader (writable table, see SQLQuery3.sql) ----
public class TransHeader
{
    public long HeaderId { get; set; }
    public long? DeviceHdrID { get; set; }
    public string? Terminal_ID { get; set; }
    public byte? TransType { get; set; }
    public int? Code { get; set; }
    public string? DocTransNo { get; set; }
    public int? TransDate { get; set; }
    public int? BranchID { get; set; }
    public int? CustomerId { get; set; }
    public int? EngineerId { get; set; }      // technician - wh_CarsTechnician.ID
    public int? CarHeaderId { get; set; }     // links to wh_CustomerCars.ID
    public int? SalesRepId { get; set; }
    public int? StoreId { get; set; }
    public double? HdrDiscount { get; set; }
    public double? HdrTax { get; set; }
    public string? Notes { get; set; }
    public byte? PayType { get; set; }
    public double? Paid { get; set; }
    public double? Total { get; set; }
    public double? NetTotal { get; set; }
    public double? Balance { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Signature { get; set; }
    public byte? Status { get; set; }
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }

    public List<TransDetail> Details { get; set; } = [];
    public List<BillAttachment> Attachments { get; set; } = [];
}

// ---- dbo.wh_TransDetails (writable table, see SQLQuery3.sql) ----
public class TransDetail
{
    public long DetailId { get; set; }
    public long HeaderId { get; set; }
    public long ItemID { get; set; }
    public string ItemBarCode { get; set; } = string.Empty;
    public int? Package { get; set; }
    public double Qty { get; set; }
    public double Price { get; set; }
    public double? DetailDiscount1 { get; set; }
    public double? DetailDiscount2 { get; set; }
    public double? DetailDiscount1Ratio { get; set; }
    public double? DetailTax { get; set; }
    public double? DetailTaxRatio { get; set; }
    public double? Value { get; set; }
    public string? DetailNotes { get; set; }
    public byte? Status { get; set; }
    public double DiamonQty { get; set; }     // NOT NULL on the real table but has a DB default of 0
    public long? InsertUserID { get; set; }
    public long? UpdateUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public long? UpdateDateTime { get; set; }

    public TransHeader? Header { get; set; }
}

// ---- dbo.wh_BillAttachments ----
public class BillAttachment
{
    public long Id { get; set; }
    public long HeaderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public string AttachmentType { get; set; } = "Photo"; // 'Photo' or 'Signature'
    public long? InsertUserID { get; set; }
    public long? InsertDateTime { get; set; }
    public byte Status { get; set; } = 1;

    public TransHeader? Header { get; set; }
}
