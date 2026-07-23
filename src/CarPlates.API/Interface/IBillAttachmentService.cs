namespace CarPlates.API.Interface;

public interface IBillAttachmentService
{
    Task<long> UploadAsync(long headerId, string fileName, Stream content, string contentType, string attachmentType, long? userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BillAttachmentDto>> GetByHeaderIdAsync(long headerId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long attachmentId, CancellationToken cancellationToken = default);
}

public record BillAttachmentDto(long Id, long HeaderId, string FileName, string FilePath, string? ContentType, long? FileSize, string AttachmentType, long? InsertUserID, long? InsertDateTime, byte Status);
