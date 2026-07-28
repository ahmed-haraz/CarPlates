using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPlates.API.Models;

[Table("fw_LOVStatments")]
public class fw_LOVStatments
{
    [Key]
    public int ID { get; set; }

    public string? LOVName_AR { get; set; }

    public string? LOVName_EN { get; set; }

    public string? TableName { get; set; }

    public string? BranchTableName { get; set; }

    public string? SQLString { get; set; }

    public int ObjectID { get; set; }

    public bool CanAdd { get; set; }

    public bool CanEdit { get; set; }

    public bool OnBranch { get; set; }

    public bool OnUser { get; set; }

    public string? MaxRecordCount { get; set; }

    public string? SearchWith { get; set; }

    public bool AllowFind { get; set; }

    public bool ShowColumns { get; set; }

    public bool canSelectAll { get; set; }

    public bool canMultiSelect { get; set; }

    public bool canCloseAfterSelect { get; set; }

    public bool canCollectQty { get; set; }

    public string? Viewname { get; set; }

    public string? Code { get; set; }

    public string? name { get; set; }

    public string? NameEng { get; set; }
}
