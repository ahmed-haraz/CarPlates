namespace CarPlates.Application.Common.Interfaces;

public enum PrintFormat
{
    Receipt,
    A4
}

public interface IReceiptPrintService
{
    Task PrintReceiptAsync(ReceiptApiResult receipt, PrintFormat format = PrintFormat.Receipt);
    Task<IReadOnlyList<string>> GetAvailablePrintersAsync();
}
