namespace CarPlates.Application.Common.Interfaces;

public interface IBillAttachmentApiService
{
    Task<List<BillAttachmentResult>> GetByHeaderIdAsync(long headerId, CancellationToken cancellationToken = default);
    Task<bool> UploadAsync(long headerId, string filePath, string attachmentType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long headerId, long attachmentId, CancellationToken cancellationToken = default);
}

public record BillAttachmentResult(long Id, long HeaderId, string FileName, string? ContentType, long? FileSize, string AttachmentType);
