namespace CarPlates.Application.Common.Interfaces;

public enum PrintFormat
{
    Receipt,
    A4,
    ReceiptViaDriver
}

public interface IReceiptPrintService
{
    Task PrintReceiptAsync(ReceiptApiResult receipt, string? printerName = null, PrintFormat format = PrintFormat.Receipt);
    Task<IReadOnlyList<string>> GetAvailablePrintersAsync();
}
