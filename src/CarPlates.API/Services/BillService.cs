using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class BillService(ApplicationDbContext context) : IBillService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<BillDto> CreateAsync(CreateBillDto dto, string? userId, CancellationToken cancellationToken = default)
    {
        var userIdLong = long.TryParse(userId, out var uid) ? (long?)uid : null;
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var details = dto.Details.Select(d =>
        {
            var lineValue = (d.Qty * d.Price) - (d.DetailDiscount1 ?? 0) + (d.DetailTax ?? 0);

            return new TransDetail
            {
                ItemID = d.ItemID,
                ItemBarCode = d.ItemBarCode,
                Package = d.Package,
                Qty = d.Qty,
                Price = d.Price,
                DetailDiscount1 = d.DetailDiscount1,
                DetailTax = d.DetailTax,
                DetailNotes = d.DetailNotes,
                Value = lineValue,
                Status = 1,
                InsertUserID = userIdLong,
                InsertDateTime = now,
            };
        }).ToList();

        var total = details.Sum(d => d.Value ?? 0);

        var header = new TransHeader
        {
            BranchID = dto.BranchID,
            CustomerId = dto.CustomerId,
            EngineerId = dto.EngineerId,
            CarHeaderId = dto.CarHeaderId,
            StoreId = dto.StoreId,
            PayType = dto.PayType,
            Notes = dto.Notes,
            RefrenceNo = dto.RefrenceNo,
            TransDate = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd")),
            Total = total,
            NetTotal = total,
            Paid = 0,
            Balance = total,
            Status = 1,
            InsertUserID = userIdLong,
            InsertDateTime = now,
            Details = details,
        };

        _context.TransHeaders.Add(header);
        await _context.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(header, cancellationToken);
    }

    public async Task<BillDto?> GetByIdAsync(long headerId, CancellationToken cancellationToken = default)
    {
        var header = await _context.TransHeaders
            .AsNoTracking()
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.HeaderId == headerId, cancellationToken);

        return header == null ? null : await MapToDtoAsync(header, cancellationToken);
    }

    public async Task<PagedResult<BillDto>> GetAllAsync(
        int branchId, int? customerId, int? carHeaderId,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.TransHeaders.AsNoTracking().AsQueryable();

        if (branchId > 0) query = query.Where(h => h.BranchID == branchId);
        if (customerId.HasValue) query = query.Where(h => h.CustomerId == customerId);
        if (carHeaderId.HasValue) query = query.Where(h => h.CarHeaderId == carHeaderId);

        query = query.OrderByDescending(h => h.HeaderId);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = new List<BillDto>();
        foreach (var h in paged.Items)
        {
            items.Add(await MapToDtoAsync(h, cancellationToken));
        }

        return new PagedResult<BillDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    public async Task<PagedResult<BillDto>> SearchAsync(
        string? search, int? transDateFrom, int? transDateTo,
        int page, int pageSize, string? userId = null, int? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TransHeaders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(h =>
                (h.RefrenceNo != null && h.RefrenceNo.ToLower().Contains(searchLower)) ||
                (h.DocTransNo != null && h.DocTransNo.ToLower().Contains(searchLower)));
        }

        if (transDateFrom.HasValue)
            query = query.Where(h => h.TransDate >= transDateFrom);
        if (transDateTo.HasValue)
            query = query.Where(h => h.TransDate <= transDateTo);

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(h => h.BranchID == branchId);

        if (!string.IsNullOrWhiteSpace(userId) && long.TryParse(userId, out var uid))
            query = query.Where(h => h.InsertUserID == uid);

        query = query.OrderByDescending(h => h.HeaderId);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = new List<BillDto>();
        foreach (var h in paged.Items)
        {
            items.Add(await MapToDtoAsync(h, cancellationToken));
        }

        return new PagedResult<BillDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    public async Task<(int todayBills, double todayTotal)> GetTodayStatsAsync(string? userId = null, int? branchId = null, CancellationToken cancellationToken = default)
    {
        var today = int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
        var query = _context.TransHeaders.AsNoTracking()
            .Where(h => h.TransDate == today && h.Status == 1);

        if (branchId.HasValue && branchId.Value > 0)
            query = query.Where(h => h.BranchID == branchId);

        if (!string.IsNullOrWhiteSpace(userId) && long.TryParse(userId, out var uid))
            query = query.Where(h => h.InsertUserID == uid);

        var todayBills = await query.CountAsync(cancellationToken);
        var todayTotal = await query.SumAsync(h => h.NetTotal ?? 0, cancellationToken);

        return (todayBills, todayTotal);
    }

    private async Task<BillDto> MapToDtoAsync(TransHeader h, CancellationToken ct = default)
    {
        string? customerName = null;
        if (h.CustomerId.HasValue)
        {
            var customer = await _context.WhCustomers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == h.CustomerId, ct);
            customerName = customer?.Name_En ?? customer?.Name_Ar;
        }

        return new BillDto(
            h.HeaderId,
            h.DocTransNo,
            h.BranchID,
            h.CustomerId,
            h.EngineerId,
            h.CarHeaderId,
            h.Total ?? 0,
            h.NetTotal ?? 0,
            h.Paid ?? 0,
            h.Balance ?? 0,
            h.PayType,
            h.Notes,
            h.RefrenceNo,
            h.TransDate,
            customerName,
            h.Details.Select(d => new BillDetailDto(
                d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                d.DetailDiscount1, d.DetailTax, d.Value)).ToList());
    }
}
