namespace cse325_project.Services;

public static class AppDataScopes
{
    public const string Profile = "profile";
    public const string Household = "household";
    public const string Locations = "locations";
    public const string Categories = "categories";
}

public interface IAppDataChangeService
{
    event Action<string>? Changed;
    void NotifyChanged(string scope);
}

public sealed class AppDataChangeService : IAppDataChangeService
{
    public event Action<string>? Changed;

    public void NotifyChanged(string scope)
    {
        Changed?.Invoke(scope);
    }
}
