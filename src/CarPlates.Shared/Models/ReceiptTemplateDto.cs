namespace CarPlates.Shared.Models;

public class ReceiptTemplateDto
{
    public int Id { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
