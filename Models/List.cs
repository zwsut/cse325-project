using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace cse325_project.Models;
[Table("lists")]
public class List : BaseModel
{
    [PrimaryKey("list_id", false)]
    public Guid ListId { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string Name { get; set; } = "Shopping List";

    [Column("list_type")]
    public string ListType { get; set; } = "shopping";

    [Column("created_by_user")]
    public Guid CreatedByUser { get; set; }
}
