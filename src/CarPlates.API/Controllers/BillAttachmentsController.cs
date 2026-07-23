using CarPlates.API.Common;
using CarPlates.API.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/bills/{headerId:long}/attachments")]
[Authorize]
public class BillAttachmentsController(IBillAttachmentService attachmentService, IUserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(long headerId, CancellationToken cancellationToken)
    {
        var result = await attachmentService.GetByHeaderIdAsync(headerId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        long headerId,
        [FromQuery] string attachmentType = "Photo",
        CancellationToken cancellationToken = default)
    {
        var file = HttpContext.Request.Form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "No file provided" });

        if (string.IsNullOrEmpty(attachmentType) || (attachmentType != "Photo" && attachmentType != "Signature"))
            attachmentType = "Photo";

        using var stream = file.OpenReadStream();
        long.TryParse(userContext.UserId, out var userIdLong);
        var id = await attachmentService.UploadAsync(headerId, file.FileName, stream, file.ContentType, attachmentType, userIdLong, cancellationToken);

        return Ok(new { Id = id, FileName = file.FileName, AttachmentType = attachmentType });
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long headerId, long id, CancellationToken cancellationToken)
    {
        var success = await attachmentService.DeleteAsync(id, cancellationToken);
        if (!success) return NotFound();
        return NoContent();
    }
}
