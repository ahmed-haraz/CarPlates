using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using CarPlates.Shared.Extensions;
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
            var lineDiscountR1 = d.DetailDiscountR1 ?? 0d;
            var lineDiscountR2 = d.DetailDiscountR2 ?? 0d;
            var lineTax = d.DetailTax ?? 0d;
            var lineTaxR = d.DetailTaxR ?? 0d;
            var lineValue = (double)Math.Round(
                (decimal)lineQty * (decimal)linePrice
                - (decimal)lineDiscount1
                + (decimal)lineTax,
                2);

            var pkg = d.Package;
            var pkg2Qty = d.Pkg2Qty ?? 0;
            var pkg3Qty = d.Pkg3Qty ?? 0;
            var pkg1Price1 = d.Pkg1Price1 ?? 0;
            var pkg2Price1 = d.Pkg2Price1 ?? 0;
            var pkg3Price1 = d.Pkg3Price1 ?? 0;
            var pkg1Price2 = d.Pkg1Price2 ?? 0;
            var pkg2Price2 = d.Pkg2Price2 ?? 0;
            var pkg3Price2 = d.Pkg3Price2 ?? 0;

            var transPkgQty1 = pkg switch
            {
                1 => lineQty,
                2 => lineQty * pkg2Qty,
                3 => lineQty * pkg2Qty * pkg3Qty,
                _ => lineQty
            };

            var costPrice = pkg switch
            {
                1 => pkg1Price1,
                2 => pkg2Price1,
                3 => pkg3Price1,
                _ => pkg1Price1
            };

            var wholePrice = pkg switch
            {
                1 => pkg1Price2,
                2 => pkg2Price2,
                3 => pkg3Price2,
                _ => pkg1Price2
            };

            return new TransDetail
            {
                ItemID = d.ItemID,
                ItemBarCode = string.IsNullOrWhiteSpace(d.ItemBarCode) ? "" : d.ItemBarCode,
                Package = d.Package,
                Qty = lineQty,
                Price = (double)Math.Round((decimal)linePrice, 2),
                DetailDiscount1 = lineDiscount1,
                DetailDiscount2 = lineDiscount2,
                DetailDiscountR1 = lineDiscountR1,
                DetailDiscountR2 = lineDiscountR2,
                DetailTax = lineTax,
                DetailTaxR = lineTaxR,
                Value = lineValue,
                DetailNotes = d.DetailNotes ?? "",
                Status = 1,
                DiamonQty = 0,
                TransPkgQty1 = transPkgQty1,
                CostPrice = costPrice,
                TransPkgPrice1 = pkg1Price1,
                WholeProfit = 0,
                Pkg2Qty = pkg2Qty,
                Pkg3Qty = pkg3Qty,
                OriginalPrice = d.OriginalPrice ?? linePrice,
                WholePrice = wholePrice,
                InsertUserID = userIdLong,
                UpdateUserID = userIdLong,
                InsertDateTime = now,
                UpdateDateTime = now,
            };
        }).ToList();

        var total = (double)Math.Round(details.Sum(d => (decimal)(d.Value ?? 0d)), 2);

        // --- Req 1: Auto-create car in wh_customercars if not exists ---
        int? carHeaderId = dto.CarHeaderId;
        var normalizedPlate = dto.PlateNumber?.Trim().ToEnglishNumbers().ToUpperInvariant();
        if (!carHeaderId.HasValue && !string.IsNullOrWhiteSpace(dto.PlateNumber) && dto.CustomerId.HasValue)
        {
            var existingCar = await _context.CustomerCars
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.PlateNumber == normalizedPlate, cancellationToken);
            if (existingCar == null)
            {
                var newCar = new CustomerCar
                {
                    CustomerID = dto.CustomerId.Value,
                    PlateNumber = normalizedPlate,
                    VIN = dto.Vin,
                    Color = dto.Color,
                    VehicleYear = dto.VehicleYear,
                    Distance = dto.Mileage,
                    PlateType = dto.PlateType,
                    BranchID = dto.BranchID,
                    Status = 1,
                    InsertUserID = userIdLong,
                    UpdateUserID = userIdLong,
                    InsertDateTime = now,
                    UpdateDateTime = now,
                };

                if (!string.IsNullOrWhiteSpace(dto.VehicleBrand))
                {
                    var trimmed = dto.VehicleBrand.Trim();
                    newCar.CarMakesID = await _context.CarMakes.AsNoTracking()
                        .Where(m => m.Name_ar == trimmed || m.Name_en == trimmed)
                        .Select(m => (int?)m.MakeID)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(dto.VehicleModel) && newCar.CarMakesID.HasValue)
                {
                    var trimmed = dto.VehicleModel.Trim();
                    newCar.CarModelID = await _context.CarModels.AsNoTracking()
                        .Where(m => (m.Name_ar == trimmed || m.Name_en == trimmed) && m.MakeID == newCar.CarMakesID.Value)
                        .Select(m => (int?)m.ModelID)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(dto.VehicleTypeName))
                {
                    var trimmed = dto.VehicleTypeName.Trim();
                    newCar.VehicleType = await _context.VehicleTypes.AsNoTracking()
                        .Where(v => v.Name_ar == trimmed || v.Name_en == trimmed)
                        .Select(v => (int?)v.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(dto.EngineTypeName))
                {
                    var trimmed = dto.EngineTypeName.Trim();
                    newCar.EngineType = await _context.EngineTypes.AsNoTracking()
                        .Where(e => e.Name_ar == trimmed || e.Name_en == trimmed)
                        .Select(e => (int?)e.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                }

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
            CarHeaderId = 0,
            SalesRepId = salesRepId,
            StoreId = storeId,
            PayType = 2,
            HdrDiscount = 0,
            HdrTax = 0,
            Notes = dto.Notes ?? "",
            ReferenceNo = dto.ReferenceNo ?? "",
            PlateNumber = normalizedPlate ?? dto.PlateNumber,
            WorkLocationID = dto.WorkLocationID,
            TechnicianID = dto.TechnicianID,
            Signature = dto.Signature ?? "",
            Total = total,
            NetTotal = total,
            Paid = 0,
            Balance = 0,
            Benefit = 0,
            InstallmentValue = 0,
            InstallmentCount = 0,
            ShippingID = 0,
            TotalCurrency = total,
            CostCenterID = 0,
            SalesID = 0,
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
                (h.PlateNumber != null && h.PlateNumber.ToLower().Contains(searchLower)) ||
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

        string? workLocationName = null;
        if (h.WorkLocationID.HasValue)
        {
            var loc = await _context.WorkLocations.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == h.WorkLocationID.Value, ct);
            workLocationName = loc?.Name_en ?? loc?.Name_ar;
        }

        string? technicianName = null;
        if (h.TechnicianID.HasValue)
        {
            var tech = await _context.CarsTechnicians.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == h.TechnicianID.Value, ct);
            technicianName = tech?.Name_en ?? tech?.Name_ar;
        }

        string? color = null;
        string? plateType = null;
        if (h.CarHeaderId.HasValue && h.CarHeaderId.Value > 0)
        {
            var car = await _context.CustomerCars.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == h.CarHeaderId.Value, ct);
            if (car != null)
            {
                color = car.Color;
                plateType = car.PlateType;
            }
        }

        var itemIds = h.Details.Select(d => d.ItemID).Distinct().ToList();
        var itemNames = await _context.ItemBarCodes.AsNoTracking()
            .Where(i => itemIds.Contains(i.ID))
            .Select(i => new { i.ID, Name = i.Name_En ?? i.Name_Ar ?? i.ItemBarCode })
            .Distinct()
            .ToDictionaryAsync(i => (long)i.ID, i => i.Name, ct);

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
            h.PlateNumber,
            h.TransDate,
            customerName,
            h.Signature,
            workLocationName,
            technicianName,
            color,
            plateType,
            h.Details.Select(d => new BillDetailDto(
                d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                d.DetailDiscount1, d.DetailDiscount2, d.DetailDiscountR1, d.DetailDiscountR2,
                d.DetailTax, d.DetailTaxR, d.Value,
                d.TransPkgQty1, d.CostPrice, d.TransPkgPrice1, d.WholeProfit,
                d.Pkg2Qty, d.Pkg3Qty, d.OriginalPrice, d.WholePrice,
                itemNames.GetValueOrDefault(d.ItemID))).ToList());
    }
}
