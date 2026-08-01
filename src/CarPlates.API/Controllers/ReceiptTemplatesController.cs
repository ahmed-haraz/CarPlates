using CarPlates.API.Common;
using CarPlates.API.Interface;
using CarPlates.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarPlates.API.Controllers;

[ApiController]
[Route("api/v1/receipt-templates")]
[Authorize]
public class ReceiptTemplatesController(IReceiptTemplateService service, IUserContext userContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ReceiptTemplate>>> GetAll(CancellationToken cancellationToken)
    {
        var templates = await service.GetAllAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpGet("{format}")]
    public async Task<ActionResult<ReceiptTemplate>> GetByFormat(string format, CancellationToken cancellationToken)
    {
        var template = await service.GetByFormatAsync(format, cancellationToken);
        if (template == null)
            return NotFound();
        return Ok(template);
    }

    [HttpPut("{format}")]
    public async Task<ActionResult<ReceiptTemplate>> Save(
        string format,
        [FromBody] ReceiptTemplate template,
        CancellationToken cancellationToken)
    {
        template.Format = format;
        var result = await service.SaveAsync(template, userContext.UserId, cancellationToken);
        return Ok(result);
    }
}
