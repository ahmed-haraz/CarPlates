using CarPlates.API.Data;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPlates.API.Services;

public class BillAttachmentService(ApplicationDbContext context, IWebHostEnvironment env) : IBillAttachmentService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IWebHostEnvironment _env = env;

    public async Task<long> UploadAsync(long headerId, string fileName, Stream content, string contentType, string attachmentType, long? userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var attachmentDir = Path.Combine(_env.ContentRootPath, "uploads", "bills", headerId.ToString());
        Directory.CreateDirectory(attachmentDir);

        var uniqueName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var filePath = Path.Combine(attachmentDir, uniqueName);

        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        var attachment = new BillAttachment
        {
            HeaderId = headerId,
            FileName = fileName,
            FilePath = filePath,
            ContentType = contentType,
            FileSize = content.Length,
            AttachmentType = attachmentType,
            InsertUserID = userId,
            InsertDateTime = now,
            Status = 1,
        };

        _context.BillAttachments.Add(attachment);
        await _context.SaveChangesAsync(cancellationToken);
        return attachment.Id;
    }

    public async Task<IReadOnlyList<BillAttachmentDto>> GetByHeaderIdAsync(long headerId, CancellationToken cancellationToken = default)
    {
        return await _context.BillAttachments
            .AsNoTracking()
            .Where(a => a.HeaderId == headerId)
            .OrderBy(a => a.Id)
            .Select(a => new BillAttachmentDto(
                a.Id, a.HeaderId, a.FileName, a.FilePath, a.ContentType, a.FileSize, a.AttachmentType, a.InsertUserID, a.InsertDateTime, a.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(long attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await _context.BillAttachments.FindAsync(attachmentId, cancellationToken);
        if (attachment == null) return false;

        if (File.Exists(attachment.FilePath))
        {
            try { File.Delete(attachment.FilePath); } catch { }
        }

        _context.BillAttachments.Remove(attachment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
