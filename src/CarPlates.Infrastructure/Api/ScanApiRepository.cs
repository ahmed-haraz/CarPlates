using CarPlates.Application.Common.DTOs;
using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Web;

namespace CarPlates.Infrastructure.Api;

// Replaces the old SQLite-backed ScanRepository. There is no local database
// any more - every read/write goes straight to the CarPlates API, so the app
// always shows the server's current state instead of a possibly-stale local
// cache. This does mean scan history and dashboard stats require a network
// connection to load.
public class ScanApiRepository(
    IHttpClientFactory httpClientFactory,
    ILogger<ScanApiRepository> logger) : IScanRepository
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");
    private readonly ILogger<ScanApiRepository> _logger = logger;

    public async Task<ScanRecordDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"scans/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();

        var api = await response.Content.ReadFromJsonAsync<ApiScanRecordDto>(ApiJsonOptions.Default, cancellationToken);
        return api?.ToScanRecordDto();
    }

    public async Task<PaginatedResult<ScanRecordDto>> GetAllAsync(
        string? plateNumber = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrWhiteSpace(plateNumber)) query["plateNumber"] = plateNumber;
        if (startDate.HasValue) query["startDate"] = startDate.Value.ToString("o");
        if (endDate.HasValue) query["endDate"] = endDate.Value.ToString("o");
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var response = await Client.GetAsync($"scans?{query}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var api = await response.Content.ReadFromJsonAsync<ApiPagedResult<ApiScanRecordDto>>(ApiJsonOptions.Default, cancellationToken);
        if (api == null)
        {
            return new PaginatedResult<ScanRecordDto>([], 0, page, pageSize, 0);
        }

        return new PaginatedResult<ScanRecordDto>(
            api.Items.Select(s => s.ToScanRecordDto()).ToList(),
            api.TotalCount,
            api.Page,
            api.PageSize,
            api.TotalPages);
    }

    public async Task<IReadOnlyList<RecentScanDto>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"scans/recent?count={count}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var api = await response.Content.ReadFromJsonAsync<List<ApiRecentScanDto>>(ApiJsonOptions.Default, cancellationToken) ?? [];
        return api.Select(s => new RecentScanDto(s.Id, s.PlateNumber, s.VehicleBrand, s.AccessStatus, s.ScanTime)).ToList();
    }

    public async Task<IReadOnlyList<ScanRecordDto>> GetAllByPlateNumberAsync(string plateNumber, CancellationToken cancellationToken = default)
    {
        // The API's plate filter is a "contains" match, so narrow down to
        // exact matches for a specific vehicle's scan history here. This is a bounded,
        // single-vehicle lookup rather than a paged list surface, so pull one generously
        // sized page (the server's max) instead of exposing paging here too.
        var page = await GetAllAsync(plateNumber, page: 1, pageSize: 100, cancellationToken: cancellationToken);
        return page.Items
            .Where(s => string.Equals(s.PlateNumber, plateNumber, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.ScanTime)
            .ToList();
    }

    public async Task<ScanRecordDto> CreateAsync(CreateScanRecordDto dto, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            PlateNumber = dto.PlateNumber,
            PlateType = dto.PlateType,
            Confidence = dto.Confidence,
            PhotoUrl = dto.PhotoPath,
            BranchID = dto.BranchID,
            DeviceId = (string?)null,
            Latitude = (double?)null,
            Longitude = (double?)null
        };

        var response = await Client.PostAsJsonAsync("scans", request, ApiJsonOptions.Default, cancellationToken);
        response.EnsureSuccessStatusCode();

        var api = await response.Content.ReadFromJsonAsync<ApiScanRecordDto>(ApiJsonOptions.Default, cancellationToken);
        return api?.ToScanRecordDto()
            ?? new ScanRecordDto(0, dto.PlateNumber, dto.PlateType, dto.Confidence, dto.PhotoPath, DateTime.UtcNow, null, null, null, null, null);
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync("dashboard/statistics", cancellationToken);
        response.EnsureSuccessStatusCode();

        var stats = await response.Content.ReadFromJsonAsync<DashboardStatisticsDto>(ApiJsonOptions.Default, cancellationToken);
        return stats ?? new DashboardStatisticsDto(0, 0, 0, 0, 0, 0,0,0);
    }

    // Shape returned by GET /scans and /scans/{id} - kept private since it's
    // purely a wire format, mapped straight into the app's ScanRecordDto.
    private record ApiScanRecordDto(
        int Id,
        string PlateNumber,
        string PlateType,
        float Confidence,
        string? PhotoUrl,
        DateTime ScanTime,
        string? Brand,
        string? Model,
        string? Color,
        string? OwnerName,
        string? AccessStatus)
    {
        public ScanRecordDto ToScanRecordDto() => new(
            Id, PlateNumber, PlateType, Confidence, PhotoUrl, ScanTime, Brand, Model, Color, OwnerName, AccessStatus);
    }

    private record ApiRecentScanDto(int Id, string PlateNumber, string? VehicleBrand, string? AccessStatus, DateTime ScanTime);

    // Shape returned by the server's PagedResult<T> wrapper (see CarPlates.API.Models.PagedResult).
    private record ApiPagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
}
