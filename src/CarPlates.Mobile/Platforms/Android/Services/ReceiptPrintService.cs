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
    private readonly IReceiptTemplateService _templateService;

    /// <summary>Default company info used when no template is available.</summary>
    internal static string DefaultCompanyName { get; set; } = "ARKAN SERVICES";
    internal static string DefaultCompanyAddress { get; set; } = "";
    internal static string DefaultCompanyPhone { get; set; } = "";
    internal static string DefaultCompanyTaxNumber { get; set; } = "";
    internal static string DefaultFooter { get; set; } = "Thank you for your visit!";

    // Arabic defaults for bilingual printing
    internal static string DefaultCompanyNameAr { get; set; } = "أركان للخدمات";
    internal static string DefaultCompanyAddressAr { get; set; } = "";
    internal static string DefaultFooterAr { get; set; } = "شكراً لزيارتكم!";

    internal static bool IsPrintLanguageArabic =>
        Preferences.Get("print_language", 0) == 1;

    internal const int RequestEnableBluetooth = 9001;
    internal const int RequestBluetoothPermission = 9002;

    internal static TaskCompletionSource<bool>? EnableBluetoothTcs { get; set; }
    internal static TaskCompletionSource<bool>? PermissionTcs { get; set; }

    public ReceiptPrintService(IReceiptTemplateService templateService)
    {
        _templateService = templateService;
    }

    public async Task PrintReceiptAsync(ReceiptApiResult receipt, string? printerName = null, PrintFormat format = PrintFormat.Receipt)
    {
        switch (format)
        {
            case PrintFormat.A4:
                await PrintA4Async(receipt);
                break;
            case PrintFormat.ReceiptViaDriver:
                await PrintReceiptViaDriverAsync(receipt);
                break;
            case PrintFormat.PlainText:
                await PrintPlainTextAsync(receipt, printerName);
                break;
            case PrintFormat.Receipt:
            default:
                await PrintEscPosAsync(receipt, printerName);
                break;
        }
    }

    public async Task<IReadOnlyList<string>> GetAvailablePrintersAsync()
    {
        var printers = new List<string>();

        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var granted = await EnsureBluetoothConnectPermissionAsync();
                if (!granted) return printers;
            }

            var adapter = BluetoothAdapter.DefaultAdapter;
            if (adapter != null)
            {
                var paired = adapter.BondedDevices?
                    .Select(d => d.Name ?? d.Address ?? "Unknown")
                    .ToList() ?? new List<string>();
                printers.AddRange(paired);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GetAvailablePrinters] {ex.Message}");
        }

        return printers;
    }

    private async Task PrintEscPosAsync(ReceiptApiResult receipt, string? printerName = null)
    {
        var text = await BuildEscPosTextAsync(receipt);
        var data = BuildEscPosReceiptFromText(receipt, text);
        await SendToPrinterAsync(data, printerName);
    }

    private async Task PrintPlainTextAsync(ReceiptApiResult receipt, string? printerName = null)
    {
        var text = await BuildPlainTextReceiptAsync(receipt);
        var data = Encoding.UTF8.GetBytes(text);
        await SendToPrinterAsync(data, printerName);
    }

    private async Task SendToPrinterAsync(byte[] data, string? printerName = null)
    {
        if (!string.IsNullOrWhiteSpace(printerName) && printerName.Contains(':'))
        {
            await SendViaNetworkAsync(data, printerName);
            return;
        }

        await SendViaBluetoothAsync(data, printerName);
    }

    private async Task SendViaNetworkAsync(byte[] data, string address)
    {
        var parts = address.Split(':');
        var ip = parts[0].Trim();
        var port = parts.Length > 1 && int.TryParse(parts[1].Trim(), out var p) ? p : 9100;

        using var tcpClient = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await tcpClient.ConnectAsync(ip, port, cts.Token);
        await using var stream = tcpClient.GetStream();
        await stream.WriteAsync(data, cts.Token);
        await stream.FlushAsync(cts.Token);
    }

    private async Task SendViaBluetoothAsync(byte[] data, string? printerName = null)
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
        {
            bool granted = await EnsureBluetoothConnectPermissionAsync();
            if (!granted)
                throw new InvalidOperationException("Bluetooth permission denied.");
        }

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

    private static async Task<bool> EnsureBluetoothConnectPermissionAsync()
    {
        var activity = Platform.CurrentActivity;
        if (activity == null) return false;

        bool hasPermission = ContextCompat.CheckSelfPermission(activity, global::Android.Manifest.Permission.BluetoothConnect) == Permission.Granted;
        if (hasPermission) return true;

        var tcs = new TaskCompletionSource<bool>();
        PermissionTcs = tcs;

        ActivityCompat.RequestPermissions(activity,
            [global::Android.Manifest.Permission.BluetoothConnect, global::Android.Manifest.Permission.BluetoothScan],
            RequestBluetoothPermission);

        return await tcs.Task;
    }

    private async Task PrintA4Async(ReceiptApiResult receipt)
    {
        var html = await BuildA4HtmlAsync(receipt);
        PrintHtml(receipt, html, "Receipt A4");
    }

    private async Task PrintReceiptViaDriverAsync(ReceiptApiResult receipt)
    {
        var html = await BuildReceiptViaDriverHtmlAsync(receipt);
        PrintHtml(receipt, html, "Receipt");
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

    private static byte[] BuildEscPosReceiptFromText(ReceiptApiResult receipt, string textTemplate)
    {
        using var ms = new MemoryStream();
        var encoding = GetArabicEncoding();

        byte[] init = { 0x1B, 0x40 };
        byte[] arabicCP = { 0x1B, 0x74, 0x11 };
        byte[] cut = { 0x1D, 0x56, 0x00 };

        ms.Write(init, 0, init.Length);
        ms.Write(arabicCP, 0, arabicCP.Length);

        var lines = textTemplate.Split('\n');
        foreach (var line in lines)
        {
            WriteLine(ms, encoding, line.TrimEnd('\r'));
        }

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

    private async Task<string> BuildA4HtmlAsync(ReceiptApiResult receipt)
    {
        var template = await _templateService.GetTemplateAsync("A4") ?? BuiltInA4;
        return RenderHtmlTemplate(template, receipt);
    }

    private async Task<string> BuildReceiptViaDriverHtmlAsync(ReceiptApiResult receipt)
    {
        var template = await _templateService.GetTemplateAsync("Driver") ?? BuiltInDriver;
        return RenderHtmlTemplate(template, receipt);
    }

    private async Task<string> BuildPlainTextReceiptAsync(ReceiptApiResult receipt)
    {
        var template = await _templateService.GetTemplateAsync("PlainText") ?? BuiltInPlainText;
        return RenderTextTemplate(template, receipt);
    }

    private async Task<string> BuildEscPosTextAsync(ReceiptApiResult receipt)
    {
        var template = await _templateService.GetTemplateAsync("EscPos") ?? BuiltInEscPos;
        return RenderTextTemplate(template, receipt);
    }

    private string RenderHtmlTemplate(string template, ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var companyName = isArabic ? DefaultCompanyNameAr : DefaultCompanyName;
        var companyAddress = isArabic ? DefaultCompanyAddressAr : DefaultCompanyAddress;
        var footer = isArabic ? DefaultFooterAr : DefaultFooter;

        var itemsHtml = string.Join("",
            receipt.Details.Select(d =>
                $"<tr><td>{System.Net.WebUtility.HtmlEncode(d.ItemName ?? d.ItemBarCode)}</td><td>{d.Qty}</td><td>{d.Price:F2}</td><td>{(d.Value ?? 0):F2}</td></tr>"));

        var paymentsHtml = receipt.Payments.Any()
            ? $"<h3>Payments</h3><table><tr><th>Method</th><th>Amount</th></tr>{string.Join("", receipt.Payments.Select(p =>
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank Transfer", _ => "Other" };
                return $"<tr><td>{method}</td><td>{p.Amount:F2}</td></tr>";
            }))}</table>"
            : "";

        var result = template
            .Replace("{CompanyName}", companyName)
            .Replace("{CompanyAddress}", companyAddress)
            .Replace("{CompanyPhone}", DefaultCompanyPhone)
            .Replace("{CompanyTaxNumber}", DefaultCompanyTaxNumber)
            .Replace("{ReceiptNo}", System.Net.WebUtility.HtmlEncode(receipt.ReceiptNo ?? "N/A"))
            .Replace("{Date}", receipt.TransDate?.ToString() ?? "N/A")
            .Replace("{CustomerName}", System.Net.WebUtility.HtmlEncode(receipt.CustomerName ?? "N/A"))
            .Replace("{PlateNumber}", System.Net.WebUtility.HtmlEncode(receipt.PlateNumber ?? "N/A"))
            .Replace("{Location}", System.Net.WebUtility.HtmlEncode(receipt.WorkLocationName ?? "N/A"))
            .Replace("{Technician}", System.Net.WebUtility.HtmlEncode(receipt.TechnicianName ?? "N/A"))
            .Replace("{Color}", System.Net.WebUtility.HtmlEncode(receipt.Color ?? "N/A"))
            .Replace("{PlateType}", System.Net.WebUtility.HtmlEncode(receipt.PlateType ?? "N/A"))
            .Replace("{PayType}", receipt.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank Transfer", 4 => "Multiple", _ => "N/A" })
            .Replace("{Items}", itemsHtml)
            .Replace("{Payments}", paymentsHtml)
            .Replace("{Total}", receipt.Total.ToString("F2"))
            .Replace("{Paid}", receipt.Paid.ToString("F2"))
            .Replace("{Balance}", receipt.Balance.ToString("F2"))
            .Replace("{Footer}", footer)
            .Replace("{CompanyName}", companyName);

        return result;
    }

    private string RenderTextTemplate(string template, ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var companyName = isArabic ? DefaultCompanyNameAr : DefaultCompanyName;
        var companyAddress = isArabic ? DefaultCompanyAddressAr : DefaultCompanyAddress;
        var footer = isArabic ? DefaultFooterAr : DefaultFooter;

        var itemsText = string.Join("\n",
            receipt.Details.Select(d =>
                $"  {(d.ItemName ?? d.ItemBarCode),-25} {d.Qty,5:F0} {d.Price,8:F2}"));

        var paymentsText = receipt.Payments.Any()
            ? $"{new string('-', 32)}\n{string.Join("\n", receipt.Payments.Select(p =>
            {
                var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank", _ => "Other" };
                return $"  {method,-12} {p.Amount,10:F2}";
            }))}"
            : "";

        var result = template
            .Replace("{CompanyName}", companyName)
            .Replace("{CompanyAddress}", companyAddress)
            .Replace("{CompanyPhone}", DefaultCompanyPhone)
            .Replace("{CompanyTaxNumber}", DefaultCompanyTaxNumber)
            .Replace("{ReceiptNo}", receipt.ReceiptNo ?? "N/A")
            .Replace("{Date}", receipt.TransDate?.ToString() ?? "N/A")
            .Replace("{CustomerName}", receipt.CustomerName ?? "N/A")
            .Replace("{PlateNumber}", receipt.PlateNumber ?? "N/A")
            .Replace("{Location}", receipt.WorkLocationName ?? "N/A")
            .Replace("{Technician}", receipt.TechnicianName ?? "N/A")
            .Replace("{Color}", receipt.Color ?? "N/A")
            .Replace("{PlateType}", receipt.PlateType ?? "N/A")
            .Replace("{Total}", receipt.Total.ToString("F2"))
            .Replace("{Paid}", receipt.Paid.ToString("F2"))
            .Replace("{Balance}", receipt.Balance.ToString("F2"))
            .Replace("{ItemsText}", itemsText)
            .Replace("{PaymentsText}", paymentsText)
            .Replace("{Footer}", footer)
            .Replace("{CompanyName}", companyName);

        return result;
    }

    // Built-in fallback templates (used when server is unreachable)
    private const string BuiltInA4 =
        "<!DOCTYPE html>\n<html><head><meta charset='utf-8'><style>\n" +
        "  body { font-family: Arial; padding: 20px; }\n" +
        "  h1 { color: #333; text-align: center; }\n" +
        "  table { width: 100%; border-collapse: collapse; margin: 10px 0; }\n" +
        "  th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }\n" +
        "  th { background: #f5f5f5; }\n" +
        "  .total { font-weight: bold; font-size: 1.1em; }\n" +
        "</style></head><body>\n" +
        "<h1>{CompanyName}</h1>\n" +
        "<div class='header'>\n" +
        "  <p><strong>Receipt:</strong> {ReceiptNo}</p>\n" +
        "  <p><strong>Date:</strong> {Date}</p>\n" +
        "  <p><strong>Customer:</strong> {CustomerName}</p>\n" +
        "  <p><strong>Plate:</strong> {PlateNumber}</p>\n" +
        "  <p><strong>Location:</strong> {Location}</p>\n" +
        "  <p><strong>Technician:</strong> {Technician}</p>\n" +
        "</div>\n" +
        "<h3>Items</h3>\n" +
        "<table><tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>{Items}</table>\n" +
        "{Payments}\n" +
        "<hr/>\n" +
        "<p class='total'>Total: {Total}</p>\n" +
        "<p class='total'>Paid: {Paid}</p>\n" +
        "<p class='total'>Balance: {Balance}</p>\n" +
        "<p style='text-align:center;margin-top:30px;color:#888;'>{Footer}</p>\n" +
        "</body></html>";

    private const string BuiltInDriver =
        "<!DOCTYPE html>\n<html><head><meta charset='utf-8'><style>\n" +
        "  body { font-family: 'Courier New', monospace; font-size: 12px; margin: 0; padding: 8px; }\n" +
        "  h2 { text-align: center; margin: 4px 0; }\n" +
        "  table { width: 100%; border-collapse: collapse; }\n" +
        "  th, td { padding: 2px 4px; text-align: left; }\n" +
        "  th { border-bottom: 1px solid #000; }\n" +
        "  .right { text-align: right; }\n" +
        "  .center { text-align: center; }\n" +
        "  .total { font-weight: bold; }\n" +
        "  .line { border-top: 1px dashed #000; margin: 4px 0; }\n" +
        "</style></head><body>\n" +
        "<h2>{CompanyName}</h2>\n" +
        "<p class='center'>Receipt: {ReceiptNo}<br/>Date: {Date}<br/>Customer: {CustomerName}<br/>Plate: {PlateNumber}</p>\n" +
        "<div class='line'></div>\n" +
        "<table><tr><th>Item</th><th>Qty</th><th>Price</th></tr>{Items}</table>\n" +
        "<div class='line'></div>\n" +
        "<table>\n" +
        "<tr class='total'><td>Total:</td><td class='right'>{Total}</td></tr>\n" +
        "<tr><td>Paid:</td><td class='right'>{Paid}</td></tr>\n" +
        "<tr><td>Balance:</td><td class='right'>{Balance}</td></tr>\n" +
        "</table>\n" +
        "{Payments}\n" +
        "<p class='center' style='margin-top:12px;'>{Footer}</p>\n" +
        "</body></html>";

    private const string BuiltInPlainText =
        "{CompanyName}\n" +
        "--------------------------------\n" +
        "Receipt: {ReceiptNo}\n" +
        "Date: {Date}\n" +
        "Customer: {CustomerName}\n" +
        "Plate: {PlateNumber}\n" +
        "Location: {Location}\n" +
        "Technician: {Technician}\n" +
        "Color: {Color}\n" +
        "Plate Type: {PlateType}\n" +
        "--------------------------------\n" +
        "  Item                      Qty    Price\n" +
        "{ItemsText}\n" +
        "--------------------------------\n" +
        "  Total:       {Total,10}\n" +
        "  Paid:        {Paid,10}\n" +
        "  Balance:     {Balance,10}\n" +
        "{PaymentsText}\n" +
        "\n" +
        "{Footer}";

    private const string BuiltInEscPos =
        "{CompanyName}\n" +
        "--------------------------------\n" +
        "Receipt: {ReceiptNo}\n" +
        "Date: {Date}\n" +
        "Customer: {CustomerName}\n" +
        "Plate: {PlateNumber}\n" +
        "Location: {Location}\n" +
        "Technician: {Technician}\n" +
        "Color: {Color}\n" +
        "Plate Type: {PlateType}\n" +
        "--------------------------------\n" +
        "  Item                      Qty    Price\n" +
        "{ItemsText}\n" +
        "--------------------------------\n" +
        "  Total:       {Total,10}\n" +
        "  Paid:        {Paid,10}\n" +
        "  Balance:     {Balance,10}\n" +
        "{PaymentsText}\n" +
        "\n" +
        "{Footer}";

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
