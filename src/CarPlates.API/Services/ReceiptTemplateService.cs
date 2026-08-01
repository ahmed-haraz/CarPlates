using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class ReceiptTemplateService(ApplicationDbContext context) : IReceiptTemplateService
{
    public async Task<List<ReceiptTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var templates = await context.ReceiptTemplates.ToListAsync(cancellationToken);
        if (templates.Count == 0)
        {
            await SeedDefaultsAsync(cancellationToken);
            templates = await context.ReceiptTemplates.ToListAsync(cancellationToken);
        }
        return templates;
    }

    public async Task<ReceiptTemplate?> GetByFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        var template = await context.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.Format == format, cancellationToken);

        if (template == null)
        {
            await SeedDefaultsAsync(cancellationToken);
            template = await context.ReceiptTemplates
                .FirstOrDefaultAsync(t => t.Format == format, cancellationToken);
        }

        return template;
    }

    public async Task<ReceiptTemplate> SaveAsync(ReceiptTemplate template, string? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await context.ReceiptTemplates
            .FirstOrDefaultAsync(t => t.Format == template.Format, cancellationToken);

        if (existing != null)
        {
            existing.Content = template.Content;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? template.UpdatedBy : updatedBy;
        }
        else
        {
            template.UpdatedAt = DateTime.UtcNow;
            template.UpdatedBy = string.IsNullOrWhiteSpace(updatedBy) ? template.UpdatedBy : updatedBy;
            context.ReceiptTemplates.Add(template);
            existing = template;
        }

        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task SeedDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var any = await context.ReceiptTemplates.AnyAsync(cancellationToken);
        if (any) return;

        var defaults = new List<ReceiptTemplate>
        {
            new()
            {
                Format = "A4",
                Content = DefaultA4Template,
                UpdatedBy = "system"
            },
            new()
            {
                Format = "Driver",
                Content = DefaultDriverTemplate,
                UpdatedBy = "system"
            },
            new()
            {
                Format = "PlainText",
                Content = DefaultPlainTextTemplate,
                UpdatedBy = "system"
            },
            new()
            {
                Format = "EscPos",
                Content = DefaultEscPosTemplate,
                UpdatedBy = "system"
            },
            // Arabic variants
            new()
            {
                Format = "A4_ar",
                Content = DefaultA4TemplateAr,
                UpdatedBy = "system"
            },
            new()
            {
                Format = "Driver_ar",
                Content = DefaultDriverTemplateAr,
                UpdatedBy = "system"
            },
            new()
            {
                Format = "PlainText_ar",
                Content = DefaultPlainTextTemplateAr,
                UpdatedBy = "system"
            },
            new()
            {
                Format = "EscPos_ar",
                Content = DefaultEscPosTemplateAr,
                UpdatedBy = "system"
            }
        };

        context.ReceiptTemplates.AddRange(defaults);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static readonly string DefaultA4Template = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body {{ font-family: Arial; padding: 20px; }}
  h1 {{ color: #333; text-align: center; }}
  table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
  th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
  th {{ background: #f5f5f5; }}
  .total {{ font-weight: bold; font-size: 1.1em; }}
  .header {{ margin-bottom: 20px; }}
</style></head><body>
<h1>{CompanyName}</h1>
<p style='text-align:center;color:#666;'>{CompanyAddress}</p>
<p style='text-align:center;color:#666;'>Tel: {CompanyPhone} | Tax: {CompanyTaxNumber}</p>
<div class='header'>
  <p><strong>Receipt:</strong> {ReceiptNo}</p>
  <p><strong>Date:</strong> {Date}</p>
  <p><strong>Customer:</strong> {CustomerName}</p>
  <p><strong>Plate:</strong> {PlateNumber}</p>
  <p><strong>Location:</strong> {Location}</p>
  <p><strong>Technician:</strong> {Technician}</p>
  <p><strong>Color:</strong> {Color}</p>
  <p><strong>Plate Type:</strong> {PlateType}</p>
  <p><strong>Pay Type:</strong> {PayType}</p>
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
</body></html>".Replace("{{", "{").Replace("}}", "}");

    private static readonly string DefaultDriverTemplate = @"<!DOCTYPE html>
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
</body></html>".Replace("{{", "{").Replace("}}", "}");

    private static readonly string DefaultPlainTextTemplate = @"{CompanyName}
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

    private static readonly string DefaultEscPosTemplate = @"{CompanyName}
-------------------------------
Receipt: {ReceiptNo}
Date: {Date}
Customer: {CustomerName}
Plate: {PlateNumber}
Location: {Location}
Technician: {Technician}
Color: {Color}
Plate Type: {PlateType}
-------------------------------
  Item                      Qty    Price
{ItemsText}
-------------------------------
  Total:       {Total}
  Paid:        {Paid}
  Balance:     {Balance}
{PaymentsText}

{Footer}";

    // ----- Arabic templates -----

    private static readonly string DefaultA4TemplateAr = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body { font-family: Arial; padding: 20px; }
  h1 { color: #333; text-align: center; }
  table { width: 100%; border-collapse: collapse; margin: 10px 0; }
  th, td { border: 1px solid #ddd; padding: 8px; text-align: right; }
  th { background: #f5f5f5; }
  .total { font-weight: bold; font-size: 1.1em; }
</style></head><body>
<h1>{CompanyName}</h1>
<div class='header'>
  <p><strong>رقم الإيصال:</strong> {ReceiptNo}</p>
  <p><strong>التاريخ:</strong> {Date}</p>
  <p><strong>العميل:</strong> {CustomerName}</p>
  <p><strong>اللوحة:</strong> {PlateNumber}</p>
  <p><strong>الموقع:</strong> {Location}</p>
  <p><strong>الفني:</strong> {Technician}</p>
</div>
<h3>الأصناف</h3>
<table><tr><th>الصنف</th><th>الكمية</th><th>السعر</th><th>الإجمالي</th></tr>{Items}</table>
{Payments}
<hr/>
<p class='total'>الإجمالي: {Total}</p>
<p class='total'>المدفوع: {Paid}</p>
<p class='total'>المتبقي: {Balance}</p>
<p style='text-align:center;margin-top:30px;color:#888;'>{Footer}</p>
</body></html>";

    private static readonly string DefaultDriverTemplateAr = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
  body { font-family: 'Courier New', monospace; font-size: 12px; margin: 0; padding: 8px; }
  h2 { text-align: center; margin: 4px 0; }
  table { width: 100%; border-collapse: collapse; }
  th, td { padding: 2px 4px; text-align: right; }
  th { border-bottom: 1px solid #000; }
  .right { text-align: left; }
  .center { text-align: center; }
  .total { font-weight: bold; }
  .line { border-top: 1px dashed #000; margin: 4px 0; }
</style></head><body>
<h2>{CompanyName}</h2>
<p class='center'>رقم الإيصال: {ReceiptNo}<br/>التاريخ: {Date}<br/>العميل: {CustomerName}<br/>اللوحة: {PlateNumber}</p>
<div class='line'></div>
<table><tr><th>الصنف</th><th>الكمية</th><th>السعر</th></tr>{Items}</table>
<div class='line'></div>
<table>
<tr class='total'><td>الإجمالي:</td><td class='right'>{Total}</td></tr>
<tr><td>المدفوع:</td><td class='right'>{Paid}</td></tr>
<tr><td>المتبقي:</td><td class='right'>{Balance}</td></tr>
</table>
{Payments}
<p class='center' style='margin-top:12px;'>{Footer}</p>
</body></html>";

    private static readonly string DefaultPlainTextTemplateAr = @"{CompanyName}
-------------------------------
رقم الإيصال: {ReceiptNo}
التاريخ: {Date}
العميل: {CustomerName}
اللوحة: {PlateNumber}
الموقع: {Location}
الفني: {Technician}
اللون: {Color}
نوع اللوحة: {PlateType}
-------------------------------
  الكمية   السعر       الصنف
{ItemsText}
-------------------------------
  الإجمالي:       {Total}
  المدفوع:        {Paid}
  المتبقي:        {Balance}
{PaymentsText}

{Footer}";

    private static readonly string DefaultEscPosTemplateAr = @"{CompanyName}
-------------------------------
رقم الإيصال: {ReceiptNo}
التاريخ: {Date}
العميل: {CustomerName}
اللوحة: {PlateNumber}
الموقع: {Location}
الفني: {Technician}
اللون: {Color}
نوع اللوحة: {PlateType}
-------------------------------
  الكمية   السعر       الصنف
{ItemsText}
-------------------------------
  الإجمالي:       {Total}
  المدفوع:        {Paid}
  المتبقي:        {Balance}
{PaymentsText}

{Footer}";
}
