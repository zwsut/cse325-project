using cse325_project.Models;

namespace cse325_project.Services;

public class ShoppingListService
{
    private readonly ISupabaseService _supabase;

    public ShoppingListService(ISupabaseService supabase) => _supabase = supabase;

    public async Task<Guid?> GetUserGroupIdAsync(Guid userId)
    {
        await _supabase.InitializeAsync();
        var res = await _supabase.Client.From<GroupMember>()
            .Where(gm => gm.UserId == userId)
            .Get();

        return res.Models.FirstOrDefault()?.GroupId;
    }

    public async Task<ListRow?> GetOrCreateShoppingListAsync(Guid groupId, Guid userId)
    {
        await _supabase.InitializeAsync();

        var lists = await _supabase.Client.From<ListRow>()
            .Where(l => l.GroupId == groupId)
            .Get();

        var existing = lists.Models.FirstOrDefault(l =>
            string.Equals(l.ListType, "shopping", StringComparison.OrdinalIgnoreCase));

        if (existing is not null) return existing;

        var inserted = await _supabase.Client.From<ListRow>().Insert(new ListRow
        {
            GroupId = groupId,
            Name = "Weekly Shopping",
            ListType = "shopping",
            CreatedByUser = userId
        });

        return inserted.Models.FirstOrDefault();
    }

    public async Task<List<ListItemRow>> GetItemsAsync(Guid listId)
    {
        await _supabase.InitializeAsync();

        var res = await _supabase.Client.From<ListItemRow>()
            .Where(i => i.ListId == listId)
            .Get();

        return res.Models
            .OrderBy(i => i.IsChecked)
            .ThenBy(i => (i.CustomName ?? ""))
            .ToList();
    }

    public async Task<Dictionary<Guid, ItemCatalogRow>> GetCatalogMapAsync()
    {
        await _supabase.InitializeAsync();
        var res = await _supabase.Client.From<ItemCatalogRow>().Get();
        return res.Models.ToDictionary(x => x.ItemId, x => x);
    }

    public async Task AddCustomAsync(Guid listId, Guid userId, string name, decimal qty, string? unit)
    {
        await _supabase.InitializeAsync();
        await _supabase.Client.From<ListItemRow>().Insert(new ListItemRow
        {
            ListId = listId,
            CustomName = name.Trim(),
            ItemId = null,
            Quantity = qty <= 0 ? 1 : qty,
            Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(),
            IsChecked = false,
            AddedByUser = userId
        });
    }

    public async Task AddCatalogAsync(Guid listId, Guid userId, Guid itemId, decimal qty, string? unit)
    {
        await _supabase.InitializeAsync();
        await _supabase.Client.From<ListItemRow>().Insert(new ListItemRow
        {
            ListId = listId,
            ItemId = itemId,
            CustomName = null,
            Quantity = qty <= 0 ? 1 : qty,
            Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim(),
            IsChecked = false,
            AddedByUser = userId
        });
    }

    public async Task ToggleCheckedAsync(ListItemRow row)
    {
        await _supabase.InitializeAsync();
        row.IsChecked = !row.IsChecked;
        await _supabase.Client.From<ListItemRow>().Update(row);
    }

    public async Task UpdateQtyUnitAsync(ListItemRow row, decimal qty, string? unit)
    {
        await _supabase.InitializeAsync();
        row.Quantity = qty <= 0 ? 1 : qty;
        row.Unit = string.IsNullOrWhiteSpace(unit) ? null : unit.Trim();
        await _supabase.Client.From<ListItemRow>().Update(row);
    }

    public async Task DeleteAsync(ListItemRow row)
    {
        await _supabase.InitializeAsync();
        await _supabase.Client.From<ListItemRow>().Delete(row);
    }
}
