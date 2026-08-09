using System.Data;
using CarPlates.API.Common;
using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class WorkshopLookupService(ApplicationDbContext context, IConfiguration configuration) : IWorkshopLookupService
{
    private readonly ApplicationDbContext _context = context;
    private readonly string _connectionString = configuration.GetConnectionString("HexaConnection")
            ?? throw new InvalidOperationException("HexaConnection string is not configured.");

    public async Task<PagedResult<TechnicianDto>> GetTechniciansAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.CarsTechnicians.AsNoTracking().Where(t => t.Status == 1);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                (t.Name_ar != null && t.Name_ar.Contains(search)) ||
                (t.Name_en != null && t.Name_en.Contains(search)) ||
                (t.Code.ToString()   != null && t.Code.ToString()!.Contains(search))
                );
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
                (w.Name_en != null && w.Name_en.Contains(search)) ||
                (w.Code.ToString() != null && w.Code.ToString()!.Contains(search)));
        }

        query = query.OrderBy(w => w.Name_en ?? w.Name_ar?? w.Code.ToString());

        var paged = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        var items = paged.Items.Select(w => new WorkLocationDto(w.Id, w.Code, w.Name_ar, w.Name_en)).ToList();

        return new PagedResult<WorkLocationDto>(items, paged.TotalCount, paged.Page, paged.PageSize, paged.TotalPages);
    }

    private static bool ShouldAutoGenerate(string? code) =>
        string.IsNullOrWhiteSpace(code) || code == "*";

    private async Task<int> ResolveCodeAsync(SqlConnection connection, string table, string? code)
    {
        if (!ShouldAutoGenerate(code) && int.TryParse(code, out var parsed))
        {
            return parsed;
        }

        await using var maxCmd = new SqlCommand($"SELECT ISNULL(MAX(Code), 0) + 1 FROM {table}", connection);
        return (int)(await maxCmd.ExecuteScalarAsync() ?? 1);
    }

    public async Task<TechnicianDto> RegisterTechnicianAsync(RegisterTechnicianRequestDto request, long? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var code = await ResolveCodeAsync(connection, "wh_CarsTechnician", request.Code);
        var now = ConverterHelper.GetDateTime();

        await using var cmd = new SqlCommand(@"
INSERT INTO wh_CarsTechnician (Code, Name_ar, Name_en, Status, InsertUserID, InsertDateTime, UpdateDateTime)
OUTPUT INSERTED.ID
VALUES (@Code, @NameAr, @NameEn, 1, @InsertUserId, @Now, @Now)", connection);

        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@NameAr", request.Name_Ar);
        cmd.Parameters.AddWithValue("@NameEn", request.Name_En);
        cmd.Parameters.AddWithValue("@InsertUserId", userId ?? 0L);
        cmd.Parameters.AddWithValue("@Now", now);

        var newId = (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Failed to insert technician."));

        return new TechnicianDto(newId, code, request.Name_Ar, request.Name_En);
    }

    public async Task<TechnicianDto> UpdateTechnicianAsync(int id, RegisterTechnicianRequestDto request, long? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var code = await ResolveCodeAsync(connection, "wh_CarsTechnician", request.Code);
        var now = ConverterHelper.GetDateTime();

        await using var cmd = new SqlCommand(@"
UPDATE wh_CarsTechnician
SET Code = @Code, Name_ar = @NameAr, Name_en = @NameEn, UpdateUserID = @UpdateUserId, UpdateDateTime = @Now
WHERE ID = @Id", connection);

        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@NameAr", request.Name_Ar);
        cmd.Parameters.AddWithValue("@NameEn", request.Name_En);
        cmd.Parameters.AddWithValue("@UpdateUserId", userId ?? 0L);
        cmd.Parameters.AddWithValue("@Now", now);

        await cmd.ExecuteNonQueryAsync();

        return new TechnicianDto(id, code, request.Name_Ar, request.Name_En);
    }

    public async Task DeleteTechnicianAsync(int id, long? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new SqlCommand(@"
UPDATE wh_CarsTechnician
SET Status = 0, UpdateUserID = @UpdateUserId, UpdateDateTime = @Now
WHERE ID = @Id", connection);

        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UpdateUserId", userId ?? 0L);
        cmd.Parameters.AddWithValue("@Now", ConverterHelper.GetDateTime());
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<WorkLocationDto> RegisterWorkLocationAsync(RegisterWorkLocationRequestDto request, long? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var code = await ResolveCodeAsync(connection, "wh_WorkLocations", request.Code);
        var now = ConverterHelper.GetDateTime();

        await using var cmd = new SqlCommand(@"
INSERT INTO wh_WorkLocations (Code, Name_ar, Name_en, Status, InsertUserID, InsertDateTime, UpdateDateTime)
OUTPUT INSERTED.ID
VALUES (@Code, @NameAr, @NameEn, 1, @InsertUserId, @Now, @Now)", connection);

        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@NameAr", request.Name_Ar);
        cmd.Parameters.AddWithValue("@NameEn", request.Name_En);
        cmd.Parameters.AddWithValue("@InsertUserId", userId ?? 0L);
        cmd.Parameters.AddWithValue("@Now", now);

        var newId = (int)(await cmd.ExecuteScalarAsync() ?? throw new InvalidOperationException("Failed to insert work location."));

        return new WorkLocationDto(newId, code, request.Name_Ar, request.Name_En);
    }

    public async Task<WorkLocationDto> UpdateWorkLocationAsync(int id, RegisterWorkLocationRequestDto request, long? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var code = await ResolveCodeAsync(connection, "wh_WorkLocations", request.Code);
        var now = ConverterHelper.GetDateTime();

        await using var cmd = new SqlCommand(@"
UPDATE wh_WorkLocations
SET Code = @Code, Name_ar = @NameAr, Name_en = @NameEn, UpdateUserID = @UpdateUserId, UpdateDateTime = @Now
WHERE ID = @Id", connection);

        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Code", code);
        cmd.Parameters.AddWithValue("@NameAr", request.Name_Ar);
        cmd.Parameters.AddWithValue("@NameEn", request.Name_En);
        cmd.Parameters.AddWithValue("@UpdateUserId", userId ?? 0L);
        cmd.Parameters.AddWithValue("@Now", now);

        await cmd.ExecuteNonQueryAsync();

        return new WorkLocationDto(id, code, request.Name_Ar, request.Name_En);
    }

    public async Task DeleteWorkLocationAsync(int id, long? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new SqlCommand(@"
UPDATE wh_WorkLocations
SET Status = 0, UpdateUserID = @UpdateUserId, UpdateDateTime = @Now
WHERE ID = @Id", connection);

        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@UpdateUserId", userId ?? 0L);
        cmd.Parameters.AddWithValue("@Now", ConverterHelper.GetDateTime());
        await cmd.ExecuteNonQueryAsync();
    }
}
