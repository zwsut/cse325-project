using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using cse325_project.Models.ViewModels;

namespace cse325_project.Services
{
    // Simple in-memory service for scaffold; replace with real DB/Supabase later.
    public class InventoryService
    {
        private readonly ConcurrentDictionary<string, InventoryLocationDto> _locations = new();

        public InventoryService()
        {
            // seed sample data
            var a = new InventoryLocationDto { Id = "loc1", MainLocation = "Home", SubLocation = "Pantry", Name = "Pantry", Slug = "pantry", Description = "Home pantry" };
            var b = new InventoryLocationDto { Id = "loc2", MainLocation = "Home", SubLocation = "Food Storage", Name = "Food Storage", Slug = "food-storage", Description = "Long term" };
            _locations[a.Id] = a;
            _locations[b.Id] = b;
        }

        public Task<IEnumerable<InventoryLocationDto>> GetLocationsAsync()
        {
            return Task.FromResult<IEnumerable<InventoryLocationDto>>(_locations.Values.OrderBy(x => x.Name).ToList());
        }

        public Task<InventoryLocationDto?> GetLocationAsync(string id)
        {
            _locations.TryGetValue(id, out var v);
            return Task.FromResult(v);
        }

        public Task SaveLocationAsync(InventoryLocationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Id)) dto.Id = System.Guid.NewGuid().ToString();
            _locations[dto.Id] = dto;
            return Task.CompletedTask;
        }

        public Task DeleteLocationAsync(string id)
        {
            _locations.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}
