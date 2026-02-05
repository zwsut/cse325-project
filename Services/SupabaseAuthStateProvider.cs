using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

public sealed class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly ISupabaseService _supabaseService;
    private bool _initialized;

    public SupabaseAuthStateProvider(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureInitializedAsync();

        var session = _supabaseService.Client.Auth.CurrentSession;
        if (session?.User is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, session.User.Id),
            new Claim(ClaimTypes.Email, session.User.Email ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "Supabase");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(string email, string password)
    {
        await EnsureInitializedAsync();
        await _supabaseService.Client.Auth.SignIn(email, password);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignUpAsync(string email, string password)
    {
        await EnsureInitializedAsync();
        await _supabaseService.Client.Auth.SignUp(email, password);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await EnsureInitializedAsync();
        await _supabaseService.Client.Auth.SignOut();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _supabaseService.InitializeAsync();
        _initialized = true;
    }
}
