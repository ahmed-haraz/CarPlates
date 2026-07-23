using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace CarPlates.Infrastructure.Api;

public class BillApiService(
    IHttpClientFactory httpClientFactory,
    ILoggingService loggingService,
    ILogger<BillApiService> logger) : IBillApiService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");
    private readonly ILoggingService _loggingService = loggingService;
    private readonly ILogger<BillApiService> _logger = logger;

    public async Task<BillApiResult> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Creating bill for car {CarHeaderId}", request.CarHeaderId);

            var response = await Client.PostAsJsonAsync("bills", request, ApiJsonOptions.Default, cancellationToken);
            stopwatch.Stop();

            _loggingService.LogApi("bills", response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return new BillApiResult(false, null, $"API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<BillResponse>(ApiJsonOptions.Default, cancellationToken);
            return new BillApiResult(true, result?.HeaderId, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi("bills", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Create bill error");
            return new BillApiResult(false, null, ex.Message);
        }
    }

    public async Task<BillSearchResult> SearchBillsAsync(string? search, int? dateFrom, int? dateTo, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var query = $"bills/search?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search))
                query += $"&search={Uri.EscapeDataString(search)}";
            if (dateFrom.HasValue)
                query += $"&transDateFrom={dateFrom}";
            if (dateTo.HasValue)
                query += $"&transDateTo={dateTo}";

            var response = await Client.GetAsync(query, cancellationToken);
            stopwatch.Stop();

            _loggingService.LogApi("bills/search", response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                return new BillSearchResult(false, [], 0, page, 1, $"API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<PagedBillResponse>(ApiJsonOptions.Default, cancellationToken);

            if (result == null)
                return new BillSearchResult(false, [], 0, page, 1, "Invalid response");

            var items = result.Items.Select(b => new BillApiItem(
                b.HeaderId, b.DocTransNo, b.BranchID, b.CustomerId, b.EngineerId,
                b.CarHeaderId, b.Total, b.NetTotal, b.Paid, b.Balance,
                b.PayType, b.Notes, b.RefrenceNo, b.TransDate, b.CustomerName, b.Signature)).ToList();

            return new BillSearchResult(true, items, result.TotalCount, result.Page, result.TotalPages, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi("bills/search", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Search bills error");
            return new BillSearchResult(false, [], 0, page, 1, ex.Message);
        }
    }

    public async Task<TodayStatsResult> GetTodayStatsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await Client.GetAsync("bills/today-stats", cancellationToken);
            stopwatch.Stop();

            _loggingService.LogApi("bills/today-stats", response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
                return new TodayStatsResult(false, 0, 0, "API error");

            var result = await response.Content.ReadFromJsonAsync<TodayStatsResponse>(ApiJsonOptions.Default, cancellationToken);
            return new TodayStatsResult(true, result?.TodayBills ?? 0, result?.TodayTotal ?? 0, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi("bills/today-stats", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Today stats error");
            return new TodayStatsResult(false, 0, 0, ex.Message);
        }
    }

    private record BillResponse(long HeaderId);
    private record TodayStatsResponse(int TodayBills, double TodayTotal);

    private class PagedBillResponse
    {
        public List<BillDtoInternal> Items { get; set; } = [];
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    private class BillDtoInternal
    {
        public long HeaderId { get; set; }
        public string? DocTransNo { get; set; }
        public int? BranchID { get; set; }
        public int? CustomerId { get; set; }
        public int? EngineerId { get; set; }
        public int? CarHeaderId { get; set; }
        public double Total { get; set; }
        public double NetTotal { get; set; }
        public double Paid { get; set; }
        public double Balance { get; set; }
        public byte? PayType { get; set; }
        public string? Notes { get; set; }
        public string? RefrenceNo { get; set; }
        public int? TransDate { get; set; }
        public string? CustomerName { get; set; }
        public string? Signature { get; set; }
    }
}
