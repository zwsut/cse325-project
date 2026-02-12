using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace cse325_project.Models;

[Table("groups")]
public class Group : BaseModel
{
    [PrimaryKey("group_id", false)]
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("created_by_user")]
    public Guid CreatedByUser { get; set; }
}
