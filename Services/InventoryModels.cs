using System.ComponentModel.DataAnnotations;

namespace cse325_project.Services
{
    public class InventoryLocationDto
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
