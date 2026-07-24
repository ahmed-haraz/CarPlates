using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Print;
using Android.Webkit;
using Android.Widget;
using CarPlates.Application.Common.Interfaces;
using Java.IO;
using Java.Util;

namespace CarPlates.Mobile.Platforms.Android.Services;

public class ReceiptPrintService : IReceiptPrintService
{
    public async Task PrintReceiptAsync(ReceiptApiResult receipt, PrintFormat format = PrintFormat.Receipt)
    {
        if (format == PrintFormat.A4)
        {
            PrintA4(receipt);
        }
        else
        {
            await PrintEscPosAsync(receipt);
        }
    }

    public Task<IReadOnlyList<string>> GetAvailablePrintersAsync()
    {
        var adapter = BluetoothAdapter.DefaultAdapter;
        if (adapter == null)
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var paired = adapter.BondedDevices?
            .Select(d => d.Name ?? d.Address ?? "Unknown")
            .ToList() ?? new List<string>();

        return Task.FromResult<IReadOnlyList<string>>(paired);
    }

    private async Task PrintEscPosAsync(ReceiptApiResult receipt)
    {
        var devices = BluetoothAdapter.DefaultAdapter?.BondedDevices?
            .ToArray();

        if (devices == null || devices.Length == 0)
        {
            ShowToast("No paired Bluetooth printer found. Pair a printer first.");
            return;
        }

        var device = devices[0];
        BluetoothSocket? socket = null;

        try
        {
            socket = device.CreateRfcommSocketToServiceRecord(
                UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"));
            await socket.ConnectAsync();

            var outputStream = socket.OutputStream;
            var data = BuildEscPosReceipt(receipt);
            await outputStream.WriteAsync(data);
            await outputStream.FlushAsync();

            ShowToast("Receipt printed successfully");
        }
        catch (System.Exception ex)
        {
            ShowToast($"Print failed: {ex.Message}");
        }
        finally
        {
            try { socket?.Close(); } catch { }
        }
    }

    private void PrintA4(ReceiptApiResult receipt)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        var printManager = activity.GetSystemService(Context.PrintService) as PrintManager;
        if (printManager == null) return;

        var html = BuildA4Html(receipt);
        var adapter = new ReceiptPrintAdapter(activity, html);

        printManager.Print("Receipt", adapter, new PrintAttributes.Builder().Build());
    }

    private static byte[] BuildEscPosReceipt(ReceiptApiResult receipt)
    {
        using var ms = new MemoryStream();
        var writer = new StreamWriter(ms, System.Text.Encoding.UTF8);

        byte[] init = { 0x1B, 0x40 };
        byte[] center = { 0x1B, 0x61, 0x01 };
        byte[] left = { 0x1B, 0x61, 0x00 };
        byte[] boldOn = { 0x1B, 0x45, 0x01 };
        byte[] boldOff = { 0x1B, 0x45, 0x00 };
        byte[] doubleH = { 0x1B, 0x64, 0x01 };
        byte[] normal = { 0x1B, 0x64, 0x00 };
        byte[] cut = { 0x1D, 0x56, 0x00 };

        ms.Write(init, 0, init.Length);
        ms.Write(center, 0, center.Length);
        ms.Write(doubleH, 0, doubleH.Length);

        writer.WriteLine("ARKAN MAINTENANCE");
        writer.Flush();

        ms.Write(boldOff, 0, boldOff.Length);
        ms.Write(normal, 0, normal.Length);

        writer.WriteLine(new string('-', 32));
        writer.WriteLine($"Receipt: {receipt.ReceiptNo}");
        writer.WriteLine($"Date: {receipt.TransDate}");
        writer.WriteLine($"Customer: {receipt.CustomerName ?? "N/A"}");
        writer.WriteLine($"Plate: {receipt.ReferenceNo ?? "N/A"}");
        writer.WriteLine(new string('-', 32));

        ms.Write(boldOn, 0, boldOn.Length);
        writer.WriteLine($"  {"Item",-20} {"Qty",5} {"Price",8}");
        writer.Flush();
        ms.Write(boldOff, 0, boldOff.Length);

        foreach (var detail in receipt.Details)
        {
            writer.WriteLine($"  {detail.ItemBarCode,-20} {detail.Qty,5} {detail.Price,8:F2}");
            writer.Flush();
        }

        writer.WriteLine(new string('-', 32));
        ms.Write(boldOn, 0, boldOn.Length);
        writer.WriteLine($"  Total:     {receipt.Total,10:F2}");
        writer.WriteLine($"  Paid:      {receipt.Paid,10:F2}");
        writer.WriteLine($"  Balance:   {receipt.Balance,10:F2}");
        writer.Flush();
        ms.Write(boldOff, 0, boldOff.Length);

        if (receipt.Payments.Any())
        {
            writer.WriteLine(new string('-', 32));
            foreach (var p in receipt.Payments)
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank", _ => "Other" };
                writer.WriteLine($"  {method,-12} {p.Amount,10:F2}");
                writer.Flush();
            }
        }

        writer.WriteLine(string.Empty);
        writer.WriteLine("Thank you for your visit!");
        writer.Flush();

        byte[] feed = { 0x1B, 0x64, 0x04 };
        ms.Write(feed, 0, feed.Length);
        ms.Write(cut, 0, cut.Length);

        return ms.ToArray();
    }

    private static string BuildA4Html(ReceiptApiResult receipt)
    {
        var itemsHtml = string.Join("",
            receipt.Details.Select(d =>
                $"<tr><td>{System.Net.WebUtility.HtmlEncode(d.ItemBarCode)}</td><td>{d.Qty}</td><td>{d.Price:F2}</td><td>{(d.Value ?? 0):F2}</td></tr>"));

        var paymentsHtml = string.Join("",
            receipt.Payments.Select(p =>
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank Transfer", _ => "Other" };
                return $"<tr><td>{method}</td><td>{p.Amount:F2}</td></tr>";
            }));

        var payType = receipt.PayType switch
        {
            1 => "Cash",
            2 => "Visa",
            3 => "Bank Transfer",
            4 => "Multiple",
            _ => "N/A"
        };

        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body {{ font-family: Arial; padding: 20px; }}
  h1 {{ color: #333; text-align: center; }}
  table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
  th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
  th {{ background: #f5f5f5; }}
  .total {{ font-weight: bold; font-size: 1.1em; }}
  .header {{ margin-bottom: 20px; }}
</style></head><body>
<h1>ARKAN MAINTENANCE</h1>
<div class='header'>
  <p><strong>Receipt:</strong> {System.Net.WebUtility.HtmlEncode(receipt.ReceiptNo)}</p>
  <p><strong>Date:</strong> {receipt.TransDate}</p>
  <p><strong>Customer:</strong> {System.Net.WebUtility.HtmlEncode(receipt.CustomerName ?? "N/A")}</p>
  <p><strong>Plate:</strong> {System.Net.WebUtility.HtmlEncode(receipt.ReferenceNo ?? "N/A")}</p>
  <p><strong>Pay Type:</strong> {payType}</p>
</div>
<h3>Items</h3>
<table><tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>{itemsHtml}</table>
<h3>Payments</h3>
<table><tr><th>Method</th><th>Amount</th></tr>{paymentsHtml}</table>
<hr/>
<p class='total'>Total: {receipt.Total:F2}</p>
<p class='total'>Paid: {receipt.Paid:F2}</p>
<p class='total'>Balance: {receipt.Balance:F2}</p>
<p style='text-align:center;margin-top:30px;color:#888;'>Thank you for your visit!</p>
</body></html>";
    }

    private static void ShowToast(string message)
    {
        var context = global::Android.App.Application.Context;
        var toast = global::Android.Widget.Toast.MakeText(context, message, global::Android.Widget.ToastLength.Short);
        toast?.Show();
    }

    private class ReceiptPrintAdapter(Activity activity, string html) : PrintDocumentAdapter
    {
        private readonly string _html = html;

        public override void OnLayout(PrintAttributes? oldAttributes, PrintAttributes? newAttributes,
            CancellationSignal? cancellationSignal, LayoutResultCallback? callback, Bundle? extras)
        {
            if (cancellationSignal?.IsCanceled == true)
            {
                callback?.OnLayoutCancelled();
                return;
            }

            var info = new PrintDocumentInfo.Builder("receipt")
                .SetContentType(PrintContentType.Document)
                .SetPageCount(1)
                .Build();

            callback?.OnLayoutFinished(info, true);
        }

        public override void OnWrite(PageRange[]? pages, ParcelFileDescriptor? destination,
            CancellationSignal? cancellationSignal, WriteResultCallback? callback)
        {
            var webView = new global::Android.Webkit.WebView(activity);
            webView.Settings.JavaScriptEnabled = false;
            webView.LoadDataWithBaseURL(null, _html, "text/html", "UTF-8", null);

            var printAdapter = webView.CreatePrintDocumentAdapter("receipt");
            printAdapter.OnWrite(pages, destination, cancellationSignal, callback);
        }
    }
}
