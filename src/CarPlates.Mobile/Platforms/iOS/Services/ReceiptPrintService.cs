using CarPlates.Application.Common.Interfaces;

namespace CarPlates.Mobile.Platforms.iOS.Services;

public class ReceiptPrintService : IReceiptPrintService
{
    public Task PrintReceiptAsync(ReceiptApiResult receipt, PrintFormat format = PrintFormat.Receipt)
    {
        System.Diagnostics.Debug.WriteLine("Printing is not yet implemented on iOS.");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetAvailablePrintersAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }
}
