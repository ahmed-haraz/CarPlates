using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Print;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CarPlates.Application.Common.Interfaces;
using Java.Util;
using System.Net.Sockets;
using System.Text;
using Color = Android.Graphics.Color;
using Paint = Android.Graphics.Paint;

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
        var isArabic = IsPrintLanguageArabic;
        var text = await BuildEscPosTextAsync(receipt);

        if (isArabic)
        {
            // Render as image to avoid code page / font issues with Arabic text
            var data = await BuildEscPosImageAsync(text);
            await SendToPrinterAsync(data, printerName);
        }
        else
        {
            var data = BuildEscPosReceiptFromText(receipt, text);
            await SendToPrinterAsync(data, printerName);
        }
    }

    private async Task PrintPlainTextAsync(ReceiptApiResult receipt, string? printerName = null)
    {
        var text = await BuildPlainTextReceiptAsync(receipt);
        var (_, encoding) = GetCodepageSettings();
        var data = encoding.GetBytes(text);
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

    private static readonly (byte EscPosValue, string EncodingName)[] CodepageMap = new (byte EscPosValue, string EncodingName)[]
    {
        (0x00, "ibm437"),       // 0  - CP437  USA/Europe
        (0x01, "ibm850"),       // 1  - CP850  Multilingual Latin I
        (0x02, "ibm860"),       // 2  - CP860  Portuguese
        (0x06, "ibm861"),       // 6  - CP861  Icelandic
        (0x03, "ibm863"),       // 3  - CP863  Canadian French
        (0x04, "ibm865"),       // 4  - CP865  Nordic
        (0x05, "windows-1252"), // 5  - CP1252 Latin I Windows
        (0x06, "ibm866"),       // 6  - CP866  Cyrillic 2
        (0x07, "ibm852"),       // 7  - CP852  Latin II
        (0x08, "ibm858"),       // 8  - CP858  Multilingual + Euro
        (0x09, "ibm862"),       // 9  - CP862  Hebrew
        (0x0A, "ibm864"),       // 10 - CP864  Arabic PC864 [MTP-3B default]
        (0x0B, "windows-1256"), // 11 - CP1256 Arabic Windows
        (0x0C, "windows-1255"), // 12 - CP1255 Hebrew Windows
        (0x0D, "ibm737"),       // 13 - CP737  Greek
        (0x0E, "windows-1253"), // 14 - CP1253 Greek Windows
        (0x0F, "ibm857"),       // 15 - CP857  Turkish
        (0x10, "windows-1254"), // 16 - CP1254 Turkish Windows
        (0x11, "windows-1250"), // 17 - CP1250 Central Europe
        (0x12, "windows-1251"), // 18 - CP1251 Cyrillic Windows
        (0x13, "ibm874"),       // 19 - CP874  Thai
    };

    private static (byte escPosValue, Encoding encoding) GetCodepageSettings()
    {
        var idx = Preferences.Get("print_codepage", 10);
        if (idx < 0 || idx >= CodepageMap.Length)
            idx = 10;

        var (escPosValue, encodingName) = CodepageMap[idx];
        Encoding encoding;
        try { encoding = Encoding.GetEncoding(encodingName); }
        catch { encoding = Encoding.UTF8; }

        return (escPosValue, encoding);
    }

    private static byte[] BuildEscPosReceiptFromText(ReceiptApiResult receipt, string textTemplate)
    {
        using var ms = new MemoryStream();
        var (escPosValue, encoding) = GetCodepageSettings();

        byte[] init = { 0x1B, 0x40 };
        byte[] codePage = { 0x1B, 0x74, escPosValue };
        byte[] cut = { 0x1D, 0x56, 0x00 };

        ms.Write(init, 0, init.Length);
        ms.Write(codePage, 0, codePage.Length);

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

    private static void WriteLine(MemoryStream ms, Encoding encoding, string text)
    {
        var bytes = encoding.GetBytes(text + "\n");
        ms.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Render receipt text as a bitmap and convert to ESC/POS raster image data.
    /// This bypasses all code-page / font issues for Arabic glyphs.</summary>
    private static Task<byte[]> BuildEscPosImageAsync(string text)
    {
        return Task.Run(() =>
        {
            using var bitmap = RenderTextToBitmap(text);
            return BitmapToEscPosRaster(bitmap);
        });
    }

    private static Bitmap RenderTextToBitmap(string text)
    {
        const int printerWidthDots = 384; // 58mm @ 203dpi typical for MTP-3B
        const float fontSize = 24f;
        const float lineSpacing = 2f;
        const int padding = 8;

        var lines = text.Split('\n', StringSplitOptions.None);
        using var measurePaint = new Paint { TextSize = fontSize, AntiAlias = true };
        try { measurePaint.SetTypeface(Typeface.Create("sans-serif", TypefaceStyle.Bold)); } catch { }

        float maxWidth = 0;
        float totalHeight = 0;
        var lineHeights = new float[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
            {
                lineHeights[i] = measurePaint.FontSpacing;
                totalHeight += lineHeights[i];
                continue;
            }

            var widths = new float[lines[i].Length];
            measurePaint.GetTextWidths(lines[i], 0, lines[i].Length, widths);
            float lineWidth = 0;
            foreach (var w in widths) lineWidth += Math.Abs(w);
            if (lineWidth > maxWidth) maxWidth = lineWidth;

            lineHeights[i] = measurePaint.FontSpacing;
            totalHeight += lineHeights[i] + lineSpacing;
        }

        // Scale if text is wider than printer
        float scale = 1f;
        if (maxWidth > printerWidthDots - padding * 2)
            scale = (printerWidthDots - padding * 2) / maxWidth;

        int bmpWidth = (int)(maxWidth * scale) + padding * 2;
        if (bmpWidth < printerWidthDots) bmpWidth = printerWidthDots;
        int bmpHeight = (int)totalHeight + padding * 2;

        var bitmap = Bitmap.CreateBitmap(bmpWidth, bmpHeight, Bitmap.Config.Argb8888)!;
        using var canvas = new Canvas(bitmap);
        canvas.DrawColor(Color.White);

        using var paint = new Paint
        {
            TextSize = fontSize * scale,
            AntiAlias = true,
            Color = Color.Black,
            TextAlign = Paint.Align.Right
        };
        try { paint.SetTypeface(Typeface.Create("sans-serif", TypefaceStyle.Bold)); } catch { }

        float y = Math.Abs(paint.Ascent()) + padding;
        float rightX = bmpWidth - padding;
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                y += paint.FontSpacing;
                continue;
            }

            canvas.DrawText(line, rightX, y, paint);
            y += paint.FontSpacing + lineSpacing * scale;
        }

        return ToGrayscaleBitmap(bitmap);
    }

    /// <summary>Convert any Bitmap to a 1-bit black/white bitmap for printing.</summary>
    private static Bitmap ToGrayscaleBitmap(Bitmap src)
    {
        var bmp = Bitmap.CreateBitmap(src.Width, src.Height, Bitmap.Config.Rgb565)!;
        using var c = new Canvas(bmp);
        c.DrawColor(Color.White);
        using var p = new Paint { AntiAlias = true };
        c.DrawBitmap(src, 0, 0, p);
        return bmp;
    }

    /// <summary>Convert a black/white Bitmap to ESC/POS GS v 0 raster data (mode 0, 8-dot single density).</summary>
    private static byte[] BitmapToEscPosRaster(Bitmap bitmap)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        int widthBytes = (w + 7) / 8;
        int dataLen = widthBytes * h;

        byte[] data = new byte[dataLen];
        for (int y = 0; y < h; y++)
        {
            for (int xb = 0; xb < widthBytes; xb++)
            {
                byte b = 0;
                for (int bit = 0; bit < 8; bit++)
                {
                    int x = xb * 8 + bit;
                    if (x < w)
                    {
                        int pixel = bitmap.GetPixel(x, y);
                        int r = (pixel >> 16) & 0xFF;
                        int g = (pixel >> 8) & 0xFF;
                        int bl = pixel & 0xFF;
                        bool isBlack = (r + g + bl) / 3 < 128;
                        if (isBlack)
                            b |= (byte)(1 << (7 - bit));
                    }
                }
                data[y * widthBytes + xb] = b;
            }
        }

        using var ms = new MemoryStream();
        // GS v 0 m xL xH yL yH
        byte[] header = {
            0x1D, 0x76, 0x30, 0x00,
            (byte)(widthBytes & 0xFF), (byte)((widthBytes >> 8) & 0xFF),
            (byte)(h & 0xFF), (byte)((h >> 8) & 0xFF)
        };
        ms.Write(header, 0, header.Length);
        ms.Write(data, 0, data.Length);
        // Feed and cut
        byte[] feedAndCut = { 0x1B, 0x64, 0x04, 0x1D, 0x56, 0x00 };
        ms.Write(feedAndCut, 0, feedAndCut.Length);

        return ms.ToArray();
    }

    private async Task<string> BuildA4HtmlAsync(ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var fallback = isArabic ? BuiltInA4Ar : BuiltInA4;
        var format = isArabic ? "A4_ar" : "A4";
        var template = await _templateService.GetTemplateAsync(format) ?? fallback;
        return RenderHtmlTemplate(template, receipt);
    }

    private async Task<string> BuildReceiptViaDriverHtmlAsync(ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var fallback = isArabic ? BuiltInDriverAr : BuiltInDriver;
        var format = isArabic ? "Driver_ar" : "Driver";
        var template = await _templateService.GetTemplateAsync(format) ?? fallback;
        return RenderHtmlTemplate(template, receipt);
    }

    private async Task<string> BuildPlainTextReceiptAsync(ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var fallback = isArabic ? BuiltInPlainTextAr : BuiltInPlainText;
        var format = isArabic ? "PlainText_ar" : "PlainText";
        var template = await _templateService.GetTemplateAsync(format) ?? fallback;
        return RenderTextTemplate(template, receipt);
    }

    private async Task<string> BuildEscPosTextAsync(ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var fallback = isArabic ? BuiltInEscPosAr : BuiltInEscPos;
        var format = isArabic ? "EscPos_ar" : "EscPos";
        var template = await _templateService.GetTemplateAsync(format) ?? fallback;
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
            ? (isArabic
                ? $"<h3>المدفوعات</h3><table><tr><th>طريقة الدفع</th><th>المبلغ</th></tr>{string.Join("", receipt.Payments.Select(p =>
                {
                    var method = p.PayType switch { 1 => "نقداً", 2 => "فيزا", 3 => "تحويل بنكي", _ => "أخرى" };
                    return $"<tr><td>{method}</td><td>{p.Amount:F2}</td></tr>";
                }))}</table>"
                : $"<h3>Payments</h3><table><tr><th>Method</th><th>Amount</th></tr>{string.Join("", receipt.Payments.Select(p =>
                {
                    var method = p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank Transfer", _ => "Other" };
                    return $"<tr><td>{method}</td><td>{p.Amount:F2}</td></tr>";
                }))}</table>")
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
            .Replace("{PayType}", isArabic
                ? receipt.PayType switch { 1 => "نقداً", 2 => "فيزا", 3 => "تحويل بنكي", 4 => "متعدد", _ => "N/A" }
                : receipt.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank Transfer", 4 => "Multiple", _ => "N/A" })
            .Replace("{Items}", itemsHtml)
            .Replace("{Payments}", paymentsHtml)
            .Replace("{Total}", receipt.Total.ToString("F2"))
            .Replace("{Paid}", receipt.Paid.ToString("F2"))
            .Replace("{Balance}", receipt.Balance.ToString("F2"))
            .Replace("{Footer}", footer)
            .Replace("{CompanyName}", companyName);

        if (isArabic)
        {
            result = result
                .Replace("text-align: left", "text-align: right")
                .Replace("text-align:center", "text-align:center")
                .Replace("<html>", "<html dir=\"rtl\">")
                .Replace("<style>", "<style>\n  body { direction: rtl; }\n");
        }

        return result;
    }

    private string RenderTextTemplate(string template, ReceiptApiResult receipt)
    {
        var isArabic = IsPrintLanguageArabic;
        var companyName = isArabic ? DefaultCompanyNameAr : DefaultCompanyName;
        var companyAddress = isArabic ? DefaultCompanyAddressAr : DefaultCompanyAddress;
        var footer = isArabic ? DefaultFooterAr : DefaultFooter;

        var itemsText = isArabic
            ? string.Join("\n",
                receipt.Details.Select(d =>
                    $"  {d.Qty,5:F0} {d.Price,8:F2} {(d.ItemName ?? d.ItemBarCode),25}"))
            : string.Join("\n",
                receipt.Details.Select(d =>
                    $"  {(d.ItemName ?? d.ItemBarCode),-25} {d.Qty,5:F0} {d.Price,8:F2}"));

        var paymentsText = receipt.Payments.Any()
            ? $"{new string('-', 32)}\n{string.Join("\n", receipt.Payments.Select(p =>
            {
                var method = isArabic
                    ? p.PayType switch { 1 => "نقداً", 2 => "فيزا", 3 => "بنك", _ => "أخرى" }
                    : p.PayType switch { 1 => "Cash", 2 => "Visa", 3 => "Bank", _ => "Other" };
                return isArabic
                    ? $"  {p.Amount,10:F2}  {method,12}"
                    : $"  {method,-12} {p.Amount,10:F2}";
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

    private const string BuiltInA4Ar =
        "<!DOCTYPE html>\n<html><head><meta charset='utf-8'><style>\n" +
        "  body { font-family: Arial; padding: 20px; }\n" +
        "  h1 { color: #333; text-align: center; }\n" +
        "  table { width: 100%; border-collapse: collapse; margin: 10px 0; }\n" +
        "  th, td { border: 1px solid #ddd; padding: 8px; text-align: right; }\n" +
        "  th { background: #f5f5f5; }\n" +
        "  .total { font-weight: bold; font-size: 1.1em; }\n" +
        "</style></head><body>\n" +
        "<h1>{CompanyName}</h1>\n" +
        "<div class='header'>\n" +
        "  <p><strong>رقم الإيصال:</strong> {ReceiptNo}</p>\n" +
        "  <p><strong>التاريخ:</strong> {Date}</p>\n" +
        "  <p><strong>العميل:</strong> {CustomerName}</p>\n" +
        "  <p><strong>اللوحة:</strong> {PlateNumber}</p>\n" +
        "  <p><strong>الموقع:</strong> {Location}</p>\n" +
        "  <p><strong>الفني:</strong> {Technician}</p>\n" +
        "</div>\n" +
        "<h3>الأصناف</h3>\n" +
        "<table><tr><th>الصنف</th><th>الكمية</th><th>السعر</th><th>الإجمالي</th></tr>{Items}</table>\n" +
        "{Payments}\n" +
        "<hr/>\n" +
        "<p class='total'>الإجمالي: {Total}</p>\n" +
        "<p class='total'>المدفوع: {Paid}</p>\n" +
        "<p class='total'>المتبقي: {Balance}</p>\n" +
        "<p style='text-align:center;margin-top:30px;color:#888;'>{Footer}</p>\n" +
        "</body></html>";

    private const string BuiltInDriverAr =
        "<!DOCTYPE html>\n<html><head><meta charset='utf-8'><style>\n" +
        "  body { font-family: 'Courier New', monospace; font-size: 12px; margin: 0; padding: 8px; }\n" +
        "  h2 { text-align: center; margin: 4px 0; }\n" +
        "  table { width: 100%; border-collapse: collapse; }\n" +
        "  th, td { padding: 2px 4px; text-align: right; }\n" +
        "  th { border-bottom: 1px solid #000; }\n" +
        "  .right { text-align: left; }\n" +
        "  .center { text-align: center; }\n" +
        "  .total { font-weight: bold; }\n" +
        "  .line { border-top: 1px dashed #000; margin: 4px 0; }\n" +
        "</style></head><body>\n" +
        "<h2>{CompanyName}</h2>\n" +
        "<p class='center'>رقم الإيصال: {ReceiptNo}<br/>التاريخ: {Date}<br/>العميل: {CustomerName}<br/>اللوحة: {PlateNumber}</p>\n" +
        "<div class='line'></div>\n" +
        "<table><tr><th>الصنف</th><th>الكمية</th><th>السعر</th></tr>{Items}</table>\n" +
        "<div class='line'></div>\n" +
        "<table>\n" +
        "<tr class='total'><td>الإجمالي:</td><td class='right'>{Total}</td></tr>\n" +
        "<tr><td>المدفوع:</td><td class='right'>{Paid}</td></tr>\n" +
        "<tr><td>المتبقي:</td><td class='right'>{Balance}</td></tr>\n" +
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
        "  Total:       {Total}\n" +
        "  Paid:        {Paid}\n" +
        "  Balance:     {Balance}\n" +
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
        "  Total:       {Total}\n" +
        "  Paid:        {Paid}\n" +
        "  Balance:     {Balance}\n" +
        "{PaymentsText}\n" +
        "\n" +
        "{Footer}";

    private const string BuiltInPlainTextAr =
        "{CompanyName}\n" +
        "--------------------------------\n" +
        "رقم الإيصال: {ReceiptNo}\n" +
        "التاريخ: {Date}\n" +
        "العميل: {CustomerName}\n" +
        "اللوحة: {PlateNumber}\n" +
        "الموقع: {Location}\n" +
        "الفني: {Technician}\n" +
        "اللون: {Color}\n" +
        "نوع اللوحة: {PlateType}\n" +
        "--------------------------------\n" +
        "  الكمية   السعر       الصنف\n" +
        "{ItemsText}\n" +
        "--------------------------------\n" +
        "  الإجمالي:       {Total}\n" +
        "  المدفوع:        {Paid}\n" +
        "  المتبقي:        {Balance}\n" +
        "{PaymentsText}\n" +
        "\n" +
        "{Footer}";

    private const string BuiltInEscPosAr =
        "{CompanyName}\n" +
        "--------------------------------\n" +
        "رقم الإيصال: {ReceiptNo}\n" +
        "التاريخ: {Date}\n" +
        "العميل: {CustomerName}\n" +
        "اللوحة: {PlateNumber}\n" +
        "الموقع: {Location}\n" +
        "الفني: {Technician}\n" +
        "اللون: {Color}\n" +
        "نوع اللوحة: {PlateType}\n" +
        "--------------------------------\n" +
        "  الكمية   السعر       الصنف\n" +
        "{ItemsText}\n" +
        "--------------------------------\n" +
        "  الإجمالي:       {Total}\n" +
        "  المدفوع:        {Paid}\n" +
        "  المتبقي:        {Balance}\n" +
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
