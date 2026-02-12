using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace cse325_project.Models;

[Table("group_members")]
public class GroupMember : BaseModel
{
    [PrimaryKey("group_id", false)]
    [Column("group_id")]
    public Guid GroupId { get; set; }

    [PrimaryKey("user_id", false)]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("role")]
    public string Role { get; set; } = "member";
}
