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
        var now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));
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

        return new ReceiptDto(
            receiptNo,
            header.HeaderId,
            header.DocTransNo,
            header.TransDate,
            customerName,
            header.ReferenceNo,
            header.Total ?? 0,
            header.NetTotal ?? 0,
            header.Paid ?? 0,
            header.Balance ?? 0,
            header.PayType,
            payments,
            header.Details.Select(d => new BillDetailDto(
                d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                d.DetailDiscount1, d.DetailDiscount2, d.DetailDiscount1Ratio, d.DetailTax, d.DetailTaxRatio, d.Value)).ToList());
    }

    private async Task<string> GenerateReceiptNoAsync(int transDate, CancellationToken cancellationToken)
    {
        var todayCount = await _context.WhPrTrans
            .AsNoTracking()
            .CountAsync(p => p.TransDate == transDate, cancellationToken);

        return $"RCP-{transDate}-{(todayCount + 1):D4}";
    }

    private static byte DeterminePayType(IReadOnlyList<PaymentDetailDto> payments)
    {
        if (payments.Count == 1)
            return payments[0].PayType;

        return 4;
    }
}
