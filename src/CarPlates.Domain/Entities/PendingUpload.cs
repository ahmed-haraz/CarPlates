using CarPlates.Domain.Enums;

namespace CarPlates.Domain.Entities;

public class PendingUpload : BaseEntity
{
    public Guid ScanRecordId { get; private set; }
    public string PlateNumber { get; private set; } = string.Empty;
    public string? PhotoPath { get; private set; }
    public DateTime CreateAt { get; private set; } = DateTime.UtcNow;
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public UploadStatus Status { get; private set; } = UploadStatus.Pending;

    public PendingUpload() { }

    public static PendingUpload Create(Guid scanRecordId, string plateNumber, string? photoPath)
    {
        return new PendingUpload
        {
            ScanRecordId = scanRecordId,
            PlateNumber = plateNumber,
            PhotoPath = photoPath
        };
    }

    public void MarkAsProcessing() => Status = UploadStatus.Processing;
    public void MarkAsCompleted() => Status = UploadStatus.Completed;

    public void MarkAsFailed(string error)
    {
        Status = UploadStatus.Failed;
        LastError = error;
        RetryCount++;
    }
}
