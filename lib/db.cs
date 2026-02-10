using Supabase;

public sealed class SupabaseSettings
{
    public string Url { get; set; } = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? throw new ArgumentException("url environment variable not found");
    public string AnonKey { get; set; } = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? throw new ArgumentException("anon key environment variable not found");
}


public interface ISupabaseService
{
    Client Client { get; }
    Task InitializeAsync();
}

public sealed class SupabaseService : ISupabaseService
{
    private readonly SupabaseSettings _settings;
    public Client Client { get; }

    public SupabaseService(SupabaseSettings settings)
    {
        _settings = settings;

        Client = new Client(
            _settings.Url,
            _settings.AnonKey,
            new SupabaseOptions
            {
                AutoConnectRealtime = false,
                AutoRefreshToken = false
            }
        );
    }

    public async Task InitializeAsync()
    {
        await Client.InitializeAsync();
    }
}
