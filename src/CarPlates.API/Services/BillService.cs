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
            // TransDate follows the same YYYYMMDD int convention as the rest of this table.
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

        // Adding only the header is enough - EF inserts the header, then every detail with
        // its generated HeaderId, all inside the one implicit transaction SaveChangesAsync opens.
        _context.TransHeaders.Add(header);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(header);
    }

    public async Task<BillDto?> GetByIdAsync(long headerId, CancellationToken cancellationToken = default)
    {
        var header = await _context.TransHeaders
            .AsNoTracking()
            .Include(h => h.Details)
            .FirstOrDefaultAsync(h => h.HeaderId == headerId, cancellationToken);

        return header == null ? null : MapToDto(header);
    }

    public async Task<PagedResult<BillDto>> GetAllAsync(
        int? branchId, int? customerId, int? carHeaderId,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.TransHeaders.AsNoTracking().AsQueryable();

        if (branchId.HasValue) query = query.Where(h => h.BranchID == branchId);
        if (customerId.HasValue) query = query.Where(h => h.CustomerId == customerId);
        if (carHeaderId.HasValue) query = query.Where(h => h.CarHeaderId == carHeaderId);

        query = query.OrderByDescending(h => h.HeaderId);

        // List view intentionally skips Details (kept light for paging); use GetByIdAsync
        // for the full breakdown of one bill.
        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(h => MapToDto(h)).ToList();

        return new PagedResult<BillDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    private static BillDto MapToDto(TransHeader h) => new(
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
        h.Details.Select(d => new BillDetailDto(
            d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
            d.DetailDiscount1, d.DetailTax, d.Value)).ToList());
}
