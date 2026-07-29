using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPlates.API.Models;

[Table("ReceiptTemplates")]
public class ReceiptTemplate
{
    [Key]
    public int Id { get; set; }

    /// <summary>Template format key: "A4", "Driver", "PlainText", "EscPos".</summary>
    [Required, MaxLength(20)]
    public string Format { get; set; } = string.Empty;

    /// <summary>The template content with placeholders like {CompanyName}, {ReceiptNo}, etc.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}
