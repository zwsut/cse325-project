using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace cse325_project.Models.Database;

[Table("users")]
public class AppUser : BaseModel
{
    [PrimaryKey("user_id")]
    public Guid UserId { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("groups")]
public class Group : BaseModel
{
    [PrimaryKey("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("created_by_user")]
    public Guid CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("group_members")]
public class GroupMember : BaseModel
{
    [PrimaryKey("group_id")]
    public Guid GroupId { get; set; }

    [PrimaryKey("user_id")]
    public Guid UserId { get; set; }

    [Column("role")]
    public string? Role { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }
}


[Table("pantries")]
public class Pantry : BaseModel
{
    [PrimaryKey("pantry_id")]
    public Guid PantryId { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("pantry_locations")]
public class PantryLocation : BaseModel
{
    [PrimaryKey("location_id")]
    public Guid LocationId { get; set; }

    [Column("pantry_id")]
    public Guid PantryId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by_user")]
    public Guid? CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("item_categories")]
public class ItemCategory : BaseModel
{
    [PrimaryKey("category_id")]
    public Guid CategoryId { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("created_by_user")]
    public Guid? CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("item_catalog")]
public class CatalogItem : BaseModel
{
    [PrimaryKey("item_id")]
    public Guid ItemId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;


    [Column("brand")]
    public string? Brand { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("default_unit")]
    public string? DefaultUnit { get; set; }

    [Column("barcode")]
    public string? Barcode { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }
}

[Table("item_tags")]
public class ItemTag : BaseModel
{
    [PrimaryKey("tag_id")]
    public Guid TagId { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("created_by_user")]
    public Guid? CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("item_catalog_tags")]
public class CatalogItemTag : BaseModel
{
    [PrimaryKey("item_id")]
    public Guid ItemId { get; set; }

    [PrimaryKey("tag_id")]
    public Guid TagId { get; set; }
}


[Table("inventory_items")]
public class InventoryItem : BaseModel
{
    [PrimaryKey("inventory_id")]
    public Guid InventoryId { get; set; }

    [Column("pantry_id")]
    public Guid PantryId { get; set; }

    [Column("location_id")]
    public Guid? LocationId { get; set; }

    [Column("item_id")]
    public Guid? ItemId { get; set; }

    [Column("custom_name")]
    public string? CustomName { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = string.Empty;

    [Column("expires_on")]
    public DateOnly? ExpiresOn { get; set; }  // was DateTime?

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by_user")]
    public Guid? CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}


[Table("lists")]
public class AppList : BaseModel
{
    [PrimaryKey("list_id")]
    public Guid ListId { get; set; }

    [Column("group_id")]
    public Guid GroupId { get; set; }

    [Column("name")]
    public string? Name { get; set; }

    [Column("list_type")]
    public string? ListType { get; set; }

    [Column("created_by_user")]
    public Guid CreatedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }
}

[Table("list_items")]
public class AppListItem : BaseModel
{
    [PrimaryKey("list_item_id")]
    public Guid ListItemId { get; set; }

    [Column("list_id")]
    public Guid ListId { get; set; }

    [Column("item_id")]
    public Guid? ItemId { get; set; }

    [Column("custom_name")]
    public string? CustomName { get; set; }

    [Column("quantity")]
    public decimal Quantity { get; set; }

    [Column("unit")]
    public string? Unit { get; set; }

    [Column("is_checked")]
    public bool IsChecked { get; set; }

    [Column("added_by_user")]
    public Guid? AddedByUser { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
