using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPlates.API.Models;

[Table("VehicleColors")]
public class VehicleColor
{
    [Key]
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string NameAr { get; set; } = string.Empty;

    [MaxLength(7)]
    public string HexCode { get; set; } = "#000000";

    public int SortOrder { get; set; }
}
