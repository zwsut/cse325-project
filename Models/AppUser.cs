using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace cse325_project.Models;

[Table("users")]
public class AppUser : BaseModel
{
    [PrimaryKey("user_id", false)]
    public Guid UserId { get; set; }

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
