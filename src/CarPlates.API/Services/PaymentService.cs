using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class PaymentService(ApplicationDbContext context, IUserContext userContext) : IPaymentService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IUserContext _userContext = userContext;

    public async Task<PayBillResponse> PayAsync(PayBillRequest request, string? userId, CancellationToken cancellationToken = default)
    {
        var userIdLong = long.TryParse(userId, out var uid) ? uid : 0L;
        var now = ConverterHelper.GetDateTime();
        var transDate = int.Parse(DateTime.Now.ToString("yyyyMMdd"));

        var header = await _context.TransHeaders
            .FirstOrDefaultAsync(h => h.HeaderId == request.HeaderId, cancellationToken);
        if (header == null)
            return new PayBillResponse(false, "Bill not found", null, 0, 0);

        if ((header.Balance ?? 0) <= 0)
            return new PayBillResponse(false, "Bill is already fully paid", null, header.Paid ?? 0, 0);

        var totalPay = request.Payments.Sum(p => p.Amount);
        if (totalPay <= 0)
            return new PayBillResponse(false, "Payment amount must be greater than zero", null, header.Paid ?? 0, header.Balance ?? 0);

        var receiptNo = await GenerateReceiptNoAsync(transDate, cancellationToken);

        foreach (var payment in request.Payments)
        {
            var prTrans = new WhPrTrans
            {
                ReceiptNo = receiptNo,
                TransDate = transDate,
                BranchID = _userContext.BranchId,
                PrTransType = 1,
                TransVal = payment.Amount,
                InvHeaderID = request.HeaderId,
                CustomerId = header.CustomerId,
                PayType = payment.PayType,
                GlPosted = false,
                CloseStatus = false,
                CurrencyId = 1,
                CurrencyRate = 1,
                TotalCurrency = payment.Amount,
                JWQtyIn21 = 0,
                JWQtyIn18 = 0,
                Status = 1,
                InsertUserID = userIdLong,
                InsertDateTime = now,
                Serial = 0,
                Has_InvDetails = false,
                b_OpeningBal = false
            };
            _context.WhPrTrans.Add(prTrans);
        }

        var newPaid = (header.Paid ?? 0) + totalPay;
        var newBalance = Math.Max(0, (header.NetTotal ?? 0) - newPaid);

        header.Paid = newPaid;
        header.Balance = newBalance;
        header.UpdateUserID = userIdLong;
        header.UpdateDateTime = now;

        if (newBalance == 0)
            header.PayType = DeterminePayType(request.Payments);

        await _context.SaveChangesAsync(cancellationToken);

        return new PayBillResponse(true, "Payment successful", receiptNo, newPaid, newBalance);
    }

    public async Task<ReceiptDto?> GetReceiptAsync(long headerId, CancellationToken cancellationToken = default)
    {
        var header = await _context.TransHeaders
            .AsNoTracking()
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.HeaderId == headerId, cancellationToken);

        if (header == null) return null;

        string? customerName = null;
        if (header.CustomerId.HasValue)
        {
            var customer = await _context.WhCustomers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == header.CustomerId, cancellationToken);
            customerName = customer?.Name_En ?? customer?.Name_Ar;
        }

        var payments = await _context.WhPrTrans
            .AsNoTracking()
            .Where(p => p.InvHeaderID == headerId && p.Status == 1 && p.PrTransType == 1)
            .Select(p => new PaymentDetailDto(p.PayType ?? 0, p.TransVal))
            .ToListAsync(cancellationToken);

        var receiptNo = await _context.WhPrTrans
            .AsNoTracking()
            .Where(p => p.InvHeaderID == headerId)
            .OrderByDescending(p => p.ID)
            .Select(p => p.ReceiptNo)
            .FirstOrDefaultAsync(cancellationToken);

        string? workLocationName = null;
        if (header.WorkLocationID.HasValue)
        {
            var loc = await _context.WorkLocations.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == header.WorkLocationID.Value, cancellationToken);
            workLocationName = loc?.Name_en ?? loc?.Name_ar;
        }

        string? technicianName = null;
        if (header.TechnicianID.HasValue)
        {
            var tech = await _context.CarsTechnicians.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == header.TechnicianID.Value, cancellationToken);
            technicianName = tech?.Name_en ?? tech?.Name_ar;
        }

        string? color = null;
        string? plateType = null;
        if (header.CarHeaderId.HasValue && header.CarHeaderId.Value > 0)
        {
            var car = await _context.CustomerCars.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == header.CarHeaderId.Value, cancellationToken);
            if (car != null)
            {
                color = car.Color;
                plateType = car.PlateType;
            }
        }

        var itemIds = header.Details.Select(d => d.ItemID).Distinct().ToList();
        var itemNames = await _context.ItemBarCodes.AsNoTracking()
            .Where(i => itemIds.Contains(i.ID))
            .Select(i => new { i.ID, Name = i.Name_En ?? i.Name_Ar ?? i.ItemBarCode })
            .Distinct()
            .ToDictionaryAsync(i => (long)i.ID, i => i.Name, cancellationToken);

        return new ReceiptDto(
            receiptNo,
            header.HeaderId,
            header.DocTransNo,
            header.TransDate,
            customerName,
            header.ReferenceNo,
            header.PlateNumber,
            header.Total ?? 0,
            header.NetTotal ?? 0,
            header.Paid ?? 0,
            header.Balance ?? 0,
            header.PayType,
            workLocationName,
            technicianName,
            color,
            plateType,
            payments,
            header.Details.Select(d => new BillDetailDto(
                d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                d.DetailDiscount1, d.DetailDiscount2, d.DetailDiscountR1, d.DetailDiscountR2, d.DetailTax, d.DetailTaxR, d.Value,
                null, null, null, null, null, null, null, null,
                itemNames.GetValueOrDefault(d.ItemID))).ToList());
    }

    private async Task<string> GenerateReceiptNoAsync(int transDate, CancellationToken cancellationToken)
    {
        var maxNo = await _context.WhPrTrans
            .AsNoTracking()
            .MaxAsync(p => (string?)p.ReceiptNo, cancellationToken) ?? "";

        var seq = 1;
        if (!string.IsNullOrEmpty(maxNo) && maxNo.Contains('-'))
        {
            var parts = maxNo.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[^1], out var last))
                seq = last + 1;
        }

        return $"RCP-{transDate}-{seq:D4}";
    }

    private static byte DeterminePayType(IReadOnlyList<PaymentDetailDto> payments)
    {
        if (payments.Count == 1)
            return payments[0].PayType;

        return 4;
    }
}
