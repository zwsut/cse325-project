using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace cse325_project.Models;

[Table("item_catalog")]
public class ItemCatalogRow : BaseModel
{
    [PrimaryKey("item_id", false)]
    public Guid ItemId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("brand")]
    public string? Brand { get; set; }

    [Column("default_unit")]
    public string? DefaultUnit { get; set; }
}
