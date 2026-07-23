using CarPlates.Application.Common.Interfaces;
using CarPlates.Shared.Constants;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CarPlates.Infrastructure.Api;

public class BillAttachmentApiService(
    IHttpClientFactory httpClientFactory,
    ILoggingService loggingService,
    ILogger<BillAttachmentApiService> logger) : IBillAttachmentApiService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private HttpClient Client => _httpClientFactory.CreateClient("CarPlatesApi");
    private readonly ILoggingService _loggingService = loggingService;
    private readonly ILogger<BillAttachmentApiService> _logger = logger;

    public async Task<List<BillAttachmentResult>> GetByHeaderIdAsync(long headerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Client.GetAsync($"v1/bills/{headerId}/attachments", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return [];

            var result = await response.Content.ReadFromJsonAsync<List<AttachmentDto>>(ApiJsonOptions.Default, cancellationToken);
            return result?.Select(a => new BillAttachmentResult(a.Id, a.HeaderId, a.FileName, a.ContentType, a.FileSize, a.AttachmentType)).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get attachments for bill {HeaderId}", headerId);
            return [];
        }
    }

    public async Task<bool> UploadAsync(long headerId, string filePath, string attachmentType, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);

            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            var mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mime);
            content.Add(fileContent, "file", fileName);

            var url = $"v1/bills/{headerId}/attachments?attachmentType={Uri.EscapeDataString(attachmentType)}";
            var response = await Client.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload attachment for bill {HeaderId}", headerId);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(long headerId, long attachmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await Client.DeleteAsync($"v1/bills/{headerId}/attachments/{attachmentId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete attachment {AttachmentId}", attachmentId);
            return false;
        }
    }

    private record AttachmentDto(long Id, long HeaderId, string FileName, string? ContentType, long? FileSize, string AttachmentType);
}
