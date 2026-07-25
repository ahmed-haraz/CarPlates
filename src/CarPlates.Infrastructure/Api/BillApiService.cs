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
                b.PayType, b.Notes, b.ReferenceNo, b.TransDate, b.CustomerName, b.Signature)).ToList();

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

    public async Task<BillDetailResult?> GetBillByIdAsync(long headerId, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await Client.GetAsync($"bills/{headerId}", cancellationToken);
            stopwatch.Stop();

            _loggingService.LogApi($"bills/{headerId}", response.IsSuccessStatusCode, stopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode) return null;

            var dto = await response.Content.ReadFromJsonAsync<BillFullDto>(ApiJsonOptions.Default, cancellationToken);
            if (dto == null) return null;

            return new BillDetailResult(
                dto.HeaderId, dto.DocTransNo, dto.BranchID, dto.CustomerId,
                dto.EngineerId, dto.CarHeaderId, dto.Total, dto.NetTotal,
                dto.Paid, dto.Balance, dto.PayType, dto.Notes, dto.ReferenceNo,
                dto.TransDate, dto.CustomerName, dto.Signature,
                dto.Details?.Select(d => new BillLineItem(
                    d.DetailId, d.ItemID, d.ItemBarCode, d.Package, d.Qty, d.Price,
                    d.DetailDiscount1, d.DetailDiscount2, d.DetailDiscount1Ratio,
                    d.DetailTax, d.DetailTaxRatio, d.Value)).ToList() ?? []);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _loggingService.LogApi($"bills/{headerId}", false, stopwatch.ElapsedMilliseconds);
            _logger.LogError(ex, "Get bill by id error");
            return null;
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
        public string? ReferenceNo { get; set; }
        public int? TransDate { get; set; }
        public string? CustomerName { get; set; }
        public string? Signature { get; set; }
    }

    private class BillFullDto
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
        public string? ReferenceNo { get; set; }
        public int? TransDate { get; set; }
        public string? CustomerName { get; set; }
        public string? Signature { get; set; }
        public List<BillDetailDtoInternal>? Details { get; set; }
    }

    private class BillDetailDtoInternal
    {
        public long DetailId { get; set; }
        public long ItemID { get; set; }
        public string ItemBarCode { get; set; } = string.Empty;
        public int? Package { get; set; }
        public double Qty { get; set; }
        public double Price { get; set; }
        public double? DetailDiscount1 { get; set; }
        public double? DetailDiscount2 { get; set; }
        public double? DetailDiscount1Ratio { get; set; }
        public double? DetailTax { get; set; }
        public double? DetailTaxRatio { get; set; }
        public double? Value { get; set; }
    }
}
