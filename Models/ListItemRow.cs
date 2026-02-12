using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace cse325_project.Models;

[Table("list_items")]
public class ListItemRow : BaseModel
{
    [PrimaryKey("list_item_id", false)]
    public Guid ListItemId { get; set; }

    [Column("list_id")]
    public Guid ListId { get; set; }

    [Column("item_id")]
    public Guid? ItemId { get; set; }

    [Column("custom_name")]
    public string? CustomName { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; } = 1;

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("is_checked")]
    public bool IsChecked { get; set; }

    [Column("added_by_user")]
    public Guid? AddedByUser { get; set; }
}
