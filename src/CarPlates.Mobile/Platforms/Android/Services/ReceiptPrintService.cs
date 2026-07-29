using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Print;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CarPlates.Application.Common.Interfaces;
using Java.Util;
using System.Net.Sockets;
using System.Text;

namespace CarPlates.Mobile.Platforms.Android.Services;

public class ReceiptPrintService : IReceiptPrintService
{
    internal const int RequestEnableBluetooth = 9001;
    internal const int RequestBluetoothPermission = 9002;

    internal static TaskCompletionSource<bool>? EnableBluetoothTcs { get; set; }
    internal static TaskCompletionSource<bool>? PermissionTcs { get; set; }

    public async Task PrintReceiptAsync(ReceiptApiResult receipt, string? printerName = null, PrintFormat format = PrintFormat.Receipt)
    {
        switch (format)
        {
            case PrintFormat.A4:
                PrintA4(receipt);
                break;
            case PrintFormat.ReceiptViaDriver:
                PrintReceiptViaDriver(receipt);
                break;
            case PrintFormat.Receipt:
            default:
                await PrintEscPosAsync(receipt, printerName);
                break;
        }
    }

    public Task<IReadOnlyList<string>> GetAvailablePrintersAsync()
    {
        var printers = new List<string>();

        var adapter = BluetoothAdapter.DefaultAdapter;
        if (adapter != null)
        {
            var paired = adapter.BondedDevices?
                .Select(d => d.Name ?? d.Address ?? "Unknown")
                .ToList() ?? new List<string>();
            printers.AddRange(paired);
        }

        return Task.FromResult<IReadOnlyList<string>>(printers);
    }

    private async Task PrintEscPosAsync(ReceiptApiResult receipt, string? printerName = null)
    {
        if (!string.IsNullOrWhiteSpace(printerName) && printerName.Contains(':'))
        {
            await PrintViaNetworkAsync(receipt, printerName);
            return;
        }

        await PrintViaBluetoothAsync(receipt, printerName);
    }

    private async Task PrintViaNetworkAsync(ReceiptApiResult receipt, string address)
    {
        var parts = address.Split(':');
        var ip = parts[0].Trim();
        var port = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var p) ? p : 9100;

        using var tcpClient = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await tcpClient.ConnectAsync(ip, port, cts.Token);
        await using var stream = tcpClient.GetStream();
        var data = BuildEscPosReceipt(receipt);
        await stream.WriteAsync(data, cts.Token);
        await stream.FlushAsync(cts.Token);
    }

    private async Task PrintViaBluetoothAsync(ReceiptApiResult receipt, string? printerName = null)
    {
        await EnsureBluetoothEnabledAsync();

        var adapter = BluetoothAdapter.DefaultAdapter;
        if (adapter == null)
            throw new InvalidOperationException("Bluetooth is not available on this device.");

        var devices = adapter.BondedDevices?.ToArray();
        if (devices == null || devices.Length == 0)
            throw new InvalidOperationException("No paired Bluetooth printer found. Pair a printer, use a network IP, or print via driver instead.");

        BluetoothDevice? device;
        if (!string.IsNullOrWhiteSpace(printerName))
        {
            device = devices.FirstOrDefault(d =>
                d.Name?.Equals(printerName, StringComparison.OrdinalIgnoreCase) == true ||
                d.Address?.Equals(printerName, StringComparison.OrdinalIgnoreCase) == true);

            if (device == null)
                throw new InvalidOperationException($"Printer '{printerName}' not found in paired devices.");
        }
        else
        {
            device = devices[0];
        }

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
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Print failed: {ex.Message}", ex);
        }
        finally
        {
            try { socket?.Close(); } catch { }
        }
    }

    private static async Task EnsureBluetoothEnabledAsync()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
            await EnsureBluetoothConnectPermissionAsync();

        var adapter = BluetoothAdapter.DefaultAdapter;
        if (adapter == null) return;
        if (adapter.IsEnabled) return;

        try
        {
            adapter.Enable();
            await Task.Delay(500);
            if (adapter.IsEnabled) return;
        }
        catch { }

        var activity = Platform.CurrentActivity;
        if (activity == null)
            throw new InvalidOperationException("Please enable Bluetooth in settings to print via ESC/POS.");

        var tcs = new TaskCompletionSource<bool>();
        EnableBluetoothTcs = tcs;

        var intent = new Intent(BluetoothAdapter.ActionRequestEnable);
        activity.StartActivityForResult(intent, RequestEnableBluetooth);

        var enabled = await tcs.Task;

        if (!enabled)
            throw new InvalidOperationException("Bluetooth was not enabled. Please enable Bluetooth and try again.");
    }

    private static async Task EnsureBluetoothConnectPermissionAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        bool hasPermission = ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.BluetoothConnect) == Permission.Granted;
        if (hasPermission) return;

        var tcs = new TaskCompletionSource<bool>();
        PermissionTcs = tcs;

        ActivityCompat.RequestPermissions(activity,
            [global::Android.Manifest.Permission.BluetoothConnect, global::Android.Manifest.Permission.BluetoothScan],
            RequestBluetoothPermission);

        var granted = await tcs.Task;

        if (!granted)
            throw new InvalidOperationException("Bluetooth permission denied. Grant it in Settings or use network/driver print.");
    }

    private void PrintA4(ReceiptApiResult receipt)
    {
        PrintHtml(receipt, BuildA4Html(receipt), "Receipt A4");
    }

    private void PrintReceiptViaDriver(ReceiptApiResult receipt)
    {
        PrintHtml(receipt, BuildReceiptViaDriverHtml(receipt), "Receipt");
    }

    private void PrintHtml(ReceiptApiResult receipt, string html, string jobName)
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return;

        var printManager = activity.GetSystemService(Context.PrintService) as PrintManager;
        if (printManager == null) return;

        var adapter = new ReceiptPrintAdapter(activity, html);
        printManager.Print(jobName, adapter, null);
    }

    private static byte[] BuildEscPosReceipt(ReceiptApiResult receipt)
    {
        using var ms = new MemoryStream();
        var encoding = GetArabicEncoding();

        byte[] init = { 0x1B, 0x40 };
        byte[] arabicCP = { 0x1B, 0x74, 0x11 };
        byte[] center = { 0x1B, 0x61, 0x01 };
        byte[] left = { 0x1B, 0x61, 0x00 };
        byte[] boldOn = { 0x1B, 0x45, 0x01 };
        byte[] boldOff = { 0x1B, 0x45, 0x00 };
        byte[] doubleH = { 0x1B, 0x64, 0x01 };
        byte[] normal = { 0x1B, 0x64, 0x00 };
        byte[] cut = { 0x1D, 0x56, 0x00 };

        ms.Write(init, 0, init.Length);
        ms.Write(arabicCP, 0, arabicCP.Length);
        ms.Write(center, 0, center.Length);
        ms.Write(doubleH, 0, doubleH.Length);

        WriteLine(ms, encoding, "ARKAN SERVICES");

        ms.Write(boldOff, 0, boldOff.Length);
        ms.Write(normal, 0, normal.Length);

        WriteLine(ms, encoding, new string('-', 32));
        WriteLine(ms, encoding, $"Receipt: {receipt.ReceiptNo}");
        WriteLine(ms, encoding, $"Date: {receipt.TransDate}");
        WriteLine(ms, encoding, $"Customer: {receipt.CustomerName ?? "N/A"}");
        WriteLine(ms, encoding, $"Plate: {receipt.PlateNumber ?? "N/A"}");
        WriteLine(ms, encoding, $"Location: {receipt.WorkLocationName ?? "N/A"}");
        WriteLine(ms, encoding, $"Technician: {receipt.TechnicianName ?? "N/A"}");
        WriteLine(ms, encoding, $"Color: {receipt.Color ?? "N/A"}");
        WriteLine(ms, encoding, $"Plate Type: {receipt.PlateType ?? "N/A"}");
        WriteLine(ms, encoding, new string('-', 32));

        ms.Write(boldOn, 0, boldOn.Length);
        WriteLine(ms, encoding, $"  {"Item",-25} {"Qty",5} {"Price",8}");
        ms.Write(boldOff, 0, boldOff.Length);

        foreach (var detail in receipt.Details)
        {
            WriteLine(ms, encoding, $"  {(detail.ItemName ?? detail.ItemBarCode),-25} {detail.Qty,5} {detail.Price,8:F2}");
        }

        WriteLine(ms, encoding, new string('-', 32));
        ms.Write(boldOn, 0, boldOn.Length);
        WriteLine(ms, encoding, $"  Total:     {receipt.Total,10:F2}");
        WriteLine(ms, encoding, $"  Paid:      {receipt.Paid,10:F2}");
        WriteLine(ms, encoding, $"  Balance:   {receipt.Balance,10:F2}");
        ms.Write(boldOff, 0, boldOff.Length);

        if (receipt.Payments.Any())
        {
            WriteLine(ms, encoding, new string('-', 32));
            foreach (var p in receipt.Payments)
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank", _ => "Other" };
                WriteLine(ms, encoding, $"  {method,-12} {p.Amount,10:F2}");
            }
        }

        WriteLine(ms, encoding, string.Empty);
        WriteLine(ms, encoding, "Thank you for your visit!");

        byte[] feed = { 0x1B, 0x64, 0x04 };
        ms.Write(feed, 0, feed.Length);
        ms.Write(cut, 0, cut.Length);

        return ms.ToArray();
    }

    private static Encoding GetArabicEncoding()
    {
        try { return Encoding.GetEncoding("windows-1256"); }
        catch { return Encoding.UTF8; }
    }

    private static void WriteLine(MemoryStream ms, Encoding encoding, string text)
    {
        var bytes = encoding.GetBytes(text + "\n");
        ms.Write(bytes, 0, bytes.Length);
    }

    private static string BuildA4Html(ReceiptApiResult receipt)
    {
        var itemsHtml = string.Join("",
            receipt.Details.Select(d =>
                $"<tr><td>{System.Net.WebUtility.HtmlEncode(d.ItemName ?? d.ItemBarCode)}</td><td>{d.Qty}</td><td>{d.Price:F2}</td><td>{(d.Value ?? 0):F2}</td></tr>"));

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
<h1>ARKAN SERVICES</h1>
<div class='header'>
  <p><strong>Receipt:</strong> {System.Net.WebUtility.HtmlEncode(receipt.ReceiptNo)}</p>
  <p><strong>Date:</strong> {receipt.TransDate}</p>
  <p><strong>Customer:</strong> {System.Net.WebUtility.HtmlEncode(receipt.CustomerName ?? "N/A")}</p>
  <p><strong>Plate:</strong> {System.Net.WebUtility.HtmlEncode(receipt.PlateNumber ?? "N/A")}</p>
  <p><strong>Location:</strong> {System.Net.WebUtility.HtmlEncode(receipt.WorkLocationName ?? "N/A")}</p>
  <p><strong>Technician:</strong> {System.Net.WebUtility.HtmlEncode(receipt.TechnicianName ?? "N/A")}</p>
  <p><strong>Color:</strong> {System.Net.WebUtility.HtmlEncode(receipt.Color ?? "N/A")}</p>
  <p><strong>Plate Type:</strong> {System.Net.WebUtility.HtmlEncode(receipt.PlateType ?? "N/A")}</p>
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

    private static string BuildReceiptViaDriverHtml(ReceiptApiResult receipt)
    {
        var itemsHtml = string.Join("",
            receipt.Details.Select(d =>
                $"<tr><td>{System.Net.WebUtility.HtmlEncode(d.ItemName ?? d.ItemBarCode)}</td><td>{d.Qty}</td><td>{d.Price:F2}</td></tr>"));

        var paymentsHtml = string.Join("",
            receipt.Payments.Select(p =>
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank Transfer", _ => "Other" };
                return $"<tr><td>{method}</td><td style='text-align:right'>{p.Amount:F2}</td></tr>";
            }));

        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body {{ font-family: 'Courier New', monospace; font-size: 12px; margin: 0; padding: 8px; }}
  h2 {{ text-align: center; margin: 4px 0; }}
  table {{ width: 100%; border-collapse: collapse; }}
  th, td {{ padding: 2px 4px; text-align: left; }}
  th {{ border-bottom: 1px solid #000; }}
  .right {{ text-align: right; }}
  .center {{ text-align: center; }}
  .total {{ font-weight: bold; }}
  .line {{ border-top: 1px dashed #000; margin: 4px 0; }}
</style></head><body>
<h2>ARKAN SERVICES</h2>
<p class='center'>Receipt: {System.Net.WebUtility.HtmlEncode(receipt.ReceiptNo)}<br/>Date: {receipt.TransDate}<br/>Customer: {System.Net.WebUtility.HtmlEncode(receipt.CustomerName ?? "N/A")}<br/>Plate: {System.Net.WebUtility.HtmlEncode(receipt.PlateNumber ?? "N/A")}</p>
<div class='line'></div>
<table><tr><th>Item</th><th>Qty</th><th>Price</th></tr>{itemsHtml}</table>
<div class='line'></div>
<table>
<tr class='total'><td>Total:</td><td class='right'>{receipt.Total:F2}</td></tr>
<tr><td>Paid:</td><td class='right'>{receipt.Paid:F2}</td></tr>
<tr><td>Balance:</td><td class='right'>{receipt.Balance:F2}</td></tr>
</table>
{(!receipt.Payments.Any() ? "" : $@"<div class='line'></div><table>{paymentsHtml}</table>")}
<p class='center' style='margin-top:12px;'>Thank you for your visit!</p>
</body></html>";
    }

    private class ReceiptPrintAdapter : PrintDocumentAdapter
    {
        private readonly global::Android.Webkit.WebView _webView;
        private readonly PrintDocumentAdapter _innerAdapter;

        public ReceiptPrintAdapter(Activity activity, string html)
        {
            _webView = new global::Android.Webkit.WebView(activity);
            _webView.Settings.JavaScriptEnabled = false;
            _webView.LoadDataWithBaseURL(null, html, "text/html", "UTF-8", null);
            _innerAdapter = _webView.CreatePrintDocumentAdapter("receipt");
        }

        public override void OnLayout(PrintAttributes? oldAttributes, PrintAttributes? newAttributes,
            CancellationSignal? cancellationSignal, LayoutResultCallback? callback, Bundle? extras)
        {
            if (cancellationSignal?.IsCanceled == true)
            {
                callback?.OnLayoutCancelled();
                return;
            }

            _innerAdapter.OnLayout(oldAttributes, newAttributes, cancellationSignal, callback, extras);
        }

        public override void OnWrite(PageRange[]? pages, ParcelFileDescriptor? destination,
            CancellationSignal? cancellationSignal, WriteResultCallback? callback)
        {
            _innerAdapter.OnWrite(pages, destination, cancellationSignal, callback);
        }
    }
}
