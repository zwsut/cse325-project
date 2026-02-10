using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

public sealed class SupabaseAuthStateProvider : AuthenticationStateProvider
{
    private readonly ISupabaseService _supabaseService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private bool _initialized;

    public SupabaseAuthStateProvider(
        ISupabaseService supabaseService,
        IHttpContextAccessor httpContextAccessor)
    {
        _supabaseService = supabaseService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureInitializedAsync();

        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var accessToken = httpUser.FindFirst(ClaimTypes.UserData)?.Value;
            var refreshToken = httpUser.FindFirst("supabase:refresh_token")?.Value;
            if (_supabaseService.Client.Auth.CurrentSession is null &&
                !string.IsNullOrWhiteSpace(accessToken) &&
                !string.IsNullOrWhiteSpace(refreshToken))
            {
                await _supabaseService.Client.Auth.SetSession(accessToken, refreshToken, true);
            }

            return new AuthenticationState(httpUser);
        }

        var session = _supabaseService.Client.Auth.CurrentSession;
        if (session is null)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var user = session.User ?? _supabaseService.Client.Auth.CurrentUser;
        var claims = new List<Claim>();

        if (user is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
        }
        else if (!string.IsNullOrWhiteSpace(session.AccessToken))
        {
            var (userId, email) = TryGetClaimsFromJwt(session.AccessToken);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                claims.Add(new Claim(ClaimTypes.Email, email));
            }
        }

        if (claims.Count == 0)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Supabase");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task SignInAsync(string email, string password)
    {
        await EnsureInitializedAsync();
        object? signInResult = await _supabaseService.Client.Auth.SignIn(email, password);
        await EnsureSessionFromResultAsync(signInResult);

        if (_supabaseService.Client.Auth.CurrentSession is null)
        {
            throw new InvalidOperationException("Sign-in did not return a session. If email confirmations are enabled, confirm the email before signing in.");
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignUpAsync(string email, string password)
    {
        await EnsureInitializedAsync();
        object? signUpResult = await _supabaseService.Client.Auth.SignUp(email, password);
        await EnsureSessionFromResultAsync(signUpResult);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SignOutAsync()
    {
        await EnsureInitializedAsync();
        await _supabaseService.Client.Auth.SignOut();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task SendPasswordResetAsync(string email)
    {
        await EnsureInitializedAsync();
        await _supabaseService.Client.Auth.ResetPasswordForEmail(email);
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

    private async Task EnsureSessionFromResultAsync(object? authResult)
    {
        if (_supabaseService.Client.Auth.CurrentSession is not null)
        {
            return;
        }

        if (!TryGetTokensFromResult(authResult, out var accessToken, out var refreshToken))
        {
            return;
        }

        await _supabaseService.Client.Auth.SetSession(accessToken!, refreshToken!, true);
    }


    private static bool TryGetTokensFromResult(object? authResult, out string? accessToken, out string? refreshToken)
    {
        accessToken = null;
        refreshToken = null;

        if (authResult is null)
        {
            return false;
        }

        object? session = authResult;
        var sessionProperty = authResult.GetType().GetProperty("Session");
        if (sessionProperty is not null)
        {
            session = sessionProperty.GetValue(authResult);
        }

        if (session is null)
        {
            return false;
        }

        accessToken = session.GetType().GetProperty("AccessToken")?.GetValue(session) as string;
        refreshToken = session.GetType().GetProperty("RefreshToken")?.GetValue(session) as string;

        return !string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(refreshToken);
    }


    private static (string? UserId, string? Email) TryGetClaimsFromJwt(string token)
    {
        var segments = token.Split('.');
        if (segments.Length < 2)
        {
            return (null, null);
        }

        var payload = segments[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string? userId = null;
            string? email = null;

            if (root.TryGetProperty("sub", out var sub))
            {
                userId = sub.GetString();
            }

            if (root.TryGetProperty("email", out var mail))
            {
                email = mail.GetString();
            }

            return (userId, email);
        }
        catch
        {
            return (null, null);
        }
    }
}
