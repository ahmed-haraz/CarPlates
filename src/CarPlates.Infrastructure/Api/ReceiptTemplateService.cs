using System.Net.Http.Json;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using CarPlates.Shared.Models;

namespace CarPlates.Infrastructure.Api;

/// <summary>
/// Fetches receipt templates from the API server and caches them in memory.
/// Falls back to built-in defaults if the server is unreachable, so printing
/// still works offline.
/// </summary>
public class ReceiptTemplateService(IHttpClientFactory httpClientFactory) : IReceiptTemplateService
{
    private readonly SemaphoreSlim _sync = new(1, 1);
    private Dictionary<string, string>? _cache;
    private DateTime _lastFetch = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<string?> GetTemplateAsync(string format, CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_cache != null && DateTime.UtcNow - _lastFetch < CacheDuration)
                return _cache.GetValueOrDefault(format) ?? GetBuiltInFallback(format);

            try
            {
                var client = httpClientFactory.CreateClient("CarPlatesApi");
                var templates = await client.GetFromJsonAsync<List<ReceiptTemplateDto>>(
                    "receipt-templates", ApiJsonOptions.Default, cancellationToken);

                if (templates != null)
                {
                    _cache = templates.ToDictionary(t => t.Format, t => t.Content);
                    _lastFetch = DateTime.UtcNow;
                    return _cache.GetValueOrDefault(format) ?? GetBuiltInFallback(format);
                }
            }
            catch
            {
                // Server unreachable — fall through to built-in defaults
            }

            return GetBuiltInFallback(format);
        }
        finally
        {
            _sync.Release();
        }
    }

    public void InvalidateCache()
    {
        _sync.Wait();
        try
        {
            _cache = null;
            _lastFetch = DateTime.MinValue;
        }
        finally
        {
            _sync.Release();
        }
    }

    private static string? GetBuiltInFallback(string format) => format switch
    {
        "A4" => BuiltInA4,
        "Driver" => BuiltInDriver,
        "PlainText" => BuiltInPlainText,
        "EscPos" => BuiltInEscPos,
        _ => null
    };

    private const string BuiltInA4 = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body {{ font-family: Arial; padding: 20px; }}
  h1 {{ color: #333; text-align: center; }}
  table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
  th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
  th {{ background: #f5f5f5; }}
  .total {{ font-weight: bold; font-size: 1.1em; }}
</style></head><body>
<h1>{CompanyName}</h1>
<div class='header'>
  <p><strong>Receipt:</strong> {ReceiptNo}</p>
  <p><strong>Date:</strong> {Date}</p>
  <p><strong>Customer:</strong> {CustomerName}</p>
  <p><strong>Plate:</strong> {PlateNumber}</p>
  <p><strong>Location:</strong> {Location}</p>
  <p><strong>Technician:</strong> {Technician}</p>
</div>
<h3>Items</h3>
<table><tr><th>Item</th><th>Qty</th><th>Price</th><th>Total</th></tr>{Items}</table>
<h3>Payments</h3>
<table><tr><th>Method</th><th>Amount</th></tr>{Payments}</table>
<hr/>
<p class='total'>Total: {Total}</p>
<p class='total'>Paid: {Paid}</p>
<p class='total'>Balance: {Balance}</p>
<p style='text-align:center;margin-top:30px;color:#888;'>{Footer}</p>
</body></html>";

    private const string BuiltInDriver = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body {{ font-family: 'Courier New', monospace; font-size: 12px; margin: 0; padding: 8px; }}
  h2 {{ text-align: center; margin: 4px 0; }}
  table {{ width: 100%; border-collapse: collapse; }}
  th, td {{ padding: 2px 4px; text-align: left; }}
  th {{ border-bottom: 1px solid #000; }}
  .total {{ font-weight: bold; }}
  .line {{ border-top: 1px dashed #000; margin: 4px 0; }}
</style></head><body>
<h2>{CompanyName}</h2>
<p class='center'>Receipt: {ReceiptNo}<br/>Date: {Date}<br/>Customer: {CustomerName}<br/>Plate: {PlateNumber}</p>
<div class='line'></div>
<table><tr><th>Item</th><th>Qty</th><th>Price</th></tr>{Items}</table>
<div class='line'></div>
<table>
<tr class='total'><td>Total:</td><td class='right'>{Total}</td></tr>
<tr><td>Paid:</td><td class='right'>{Paid}</td></tr>
<tr><td>Balance:</td><td class='right'>{Balance}</td></tr>
</table>
{Payments}
<p class='center' style='margin-top:12px;'>{Footer}</p>
</body></html>";

    private const string BuiltInPlainText = @"{CompanyName}
--------------------------------
Receipt: {ReceiptNo}
Date: {Date}
Customer: {CustomerName}
Plate: {PlateNumber}
Location: {Location}
Technician: {Technician}
Color: {Color}
Plate Type: {PlateType}
--------------------------------
  Item                      Qty    Price
{ItemsText}
--------------------------------
  Total:       {Total}
  Paid:        {Paid}
  Balance:     {Balance}
{PaymentsText}

{Footer}";

    private const string BuiltInEscPos = @"{CompanyName}
--------------------------------
Receipt: {ReceiptNo}
Date: {Date}
Customer: {CustomerName}
Plate: {PlateNumber}
Location: {Location}
Technician: {Technician}
Color: {Color}
Plate Type: {PlateType}
--------------------------------
  Item                      Qty    Price
{ItemsText}
--------------------------------
  Total:       {Total}
  Paid:        {Paid}
  Balance:     {Balance}
{PaymentsText}

{Footer}";
}
