using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class WorkshopLookupService(ApplicationDbContext context) : IWorkshopLookupService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<PagedResult<TechnicianDto>> GetTechniciansAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CarsTechnicians.AsNoTracking().Where(t => t.Status == 1);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                (t.Name_ar != null && t.Name_ar.Contains(search)) ||
                (t.Name_en != null && t.Name_en.Contains(search)));
        }

        query = query.OrderBy(t => t.Name_en ?? t.Name_ar);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(t => new TechnicianDto(t.Id, t.Code, t.Name_ar, t.Name_en)).ToList();

        return new PagedResult<TechnicianDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    public async Task<PagedResult<WorkLocationDto>> GetWorkLocationsAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.WorkLocations.AsNoTracking().Where(w => w.Status == 1);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w =>
                (w.Name_ar != null && w.Name_ar.Contains(search)) ||
                (w.Name_en != null && w.Name_en.Contains(search)));
        }

        query = query.OrderBy(w => w.Name_en ?? w.Name_ar);

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(w => new WorkLocationDto(w.Id, w.Code, w.Name_ar, w.Name_en)).ToList();

        return new PagedResult<WorkLocationDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }
}
