using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class BillService(ApplicationDbContext context) : IBillService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<BillDto> CreateAsync(CreateBillDto dto, string? userId, IUserContext? userContext = null, CancellationToken cancellationToken = default)
    {
        var userIdLong = long.TryParse(userId, out var uid) ? uid : 0L;
        var now = long.Parse(DateTime.Now.ToString("yyyyMMddHHmmss"));

        var salesRepId = dto.SalesRepId ?? userContext?.SalesRepId ?? 0;
        var storeId = dto.StoreId ?? userContext?.StoreId ?? 0;
        var branchId = dto.BranchID ?? userContext?.BranchId ?? 0;

        var details = dto.Details.Select(d =>
        {
            var lineQty = d.Qty > 0 ? d.Qty : 1d;
            var linePrice = d.Price > 0 ? d.Price : 0d;
            var lineDiscount1 = d.DetailDiscount1 ?? 0d;
            var lineDiscount2 = d.DetailDiscount2 ?? 0d;
            var lineDiscount1Ratio = d.DetailDiscount1Ratio ?? 0d;
            var lineTax = d.DetailTax ?? 0d;
            var lineTaxRatio = d.DetailTaxRatio ?? 0d;
            var lineValue = (double)Math.Round(
                (decimal)lineQty * (decimal)linePrice
                - (decimal)lineDiscount1
                + (decimal)lineTax,
                2);

            return new TransDetail
            {
                ItemID = d.ItemID,
                ItemBarCode = string.IsNullOrWhiteSpace(d.ItemBarCode) ? "" : d.ItemBarCode,
                Package = d.Package,
                Qty = lineQty,
                Price = (double)Math.Round((decimal)linePrice, 2),
                DetailDiscount1 = lineDiscount1,
                DetailDiscount2 = lineDiscount2,
                DetailDiscount1Ratio = lineDiscount1Ratio,
                DetailTax = lineTax,
                DetailTaxRatio = lineTaxRatio,
                Value = lineValue,
                DetailNotes = d.DetailNotes ?? "",
                Status = 1,
                DiamonQty = 0,
                InsertUserID = userIdLong,
                UpdateUserID = userIdLong,
                InsertDateTime = now,
                UpdateDateTime = now,
            };
        }).ToList();

        var total = (double)Math.Round(details.Sum(d => (decimal)(d.Value ?? 0d)), 2);

        // --- Req 1: Auto-create car in wh_customercars if not exists ---
        int? carHeaderId = dto.CarHeaderId;
        if (!carHeaderId.HasValue && !string.IsNullOrWhiteSpace(dto.ReferenceNo) && dto.CustomerId.HasValue)
        {
            var existingCar = await _context.CustomerCars
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PlateNumber == dto.ReferenceNo.ToUpperInvariant(), cancellationToken);
            if (existingCar == null)
            {
                var newCar = new CustomerCar
                {
                    CustomerID = dto.CustomerId.Value,
                    PlateNumber = dto.ReferenceNo.ToUpperInvariant(),
                    Status = 1,
                    InsertUserID = userIdLong,
                    UpdateUserID = userIdLong,
                    InsertDateTime = now,
                    UpdateDateTime = now,
                };
                _context.CustomerCars.Add(newCar);
                await _context.SaveChangesAsync(cancellationToken);
                carHeaderId = (int)newCar.Id;
            }
            else
            {
                carHeaderId = (int)existingCar.Id;
            }
        }

        // --- Req 9: Auto-generate Code for TransHeader ---
        var maxCode = await _context.TransHeaders
            .MaxAsync(h => (int?)h.Code, cancellationToken) ?? 0;
        var newCode = maxCode + 1;

        var header = new TransHeader
        {
            TransType = 3,
            Code = newCode,
            TransDate = int.Parse(DateTime.Now.ToString("yyyyMMdd")),
            BranchID = branchId,
            CustomerId = dto.CustomerId ?? 0,
            EngineerId = dto.EngineerId ?? 0,
            CarHeaderId = carHeaderId ?? 0,
            SalesRepId = salesRepId,
            StoreId = storeId,
            PayType = 2,
            HdrDiscount = 0,
            HdrTax = 0,
            Notes = dto.Notes ?? "",
            ReferenceNo = dto.ReferenceNo ?? "",
            Signature = dto.Signature ?? "",
            Total = total,
            NetTotal = total,
            Paid = total,
            Balance = 0,
            Status = 1,
            InsertUserID = userIdLong,
            UpdateUserID = userIdLong,
            InsertDateTime = now,
            UpdateDateTime = now,
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
                (h.ReferenceNo != null && h.ReferenceNo.ToLower().Contains(searchLower)) ||
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
        var today = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
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
            h.ReferenceNo,
            h.TransDate,
            customerName,
            h.Signature,
            h.Details.Select(d => new BillDetailDto(
                d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                d.DetailDiscount1, d.DetailDiscount2, d.DetailDiscount1Ratio, d.DetailTax, d.DetailTaxRatio, d.Value)).ToList());
    }
}
