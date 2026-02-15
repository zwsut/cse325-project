using System.ComponentModel.DataAnnotations;

namespace cse325_project.Models.ViewModels;

public sealed class LoginModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public sealed class SignupModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class SettingsProfileModel
{
    [Required, StringLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public sealed class SettingsPasswordModel
{
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class SettingsHouseholdModel
{
    [Required, StringLength(150)]
    public string HouseholdName { get; set; } = string.Empty;
}

public sealed record InventoryEditLocationRow(
    Guid LocationId,
    Guid PantryId,
    string PantryName,
    string LocationName,
    string? Notes);

public sealed class InventoryEditLocationFormModel
{
    [Required, StringLength(100)]
    public string SubLocation { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public sealed record SidebarPantrySection(string PantryName, IReadOnlyList<SidebarLocationLink> Locations);
public sealed record SidebarLocationLink(string Name, string Slug, string? Notes);

public sealed record InventoryDisplayItem(string Id, string Name, string Amount, string Category, string Description);

public sealed class WeatherForecastModel
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public string? Summary { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public sealed class InventoryLocationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required, StringLength(100)]
    public string MainLocation { get; set; } = string.Empty;

    [StringLength(100)]
    public string? SubLocation { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}
