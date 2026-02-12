using System.Security.Claims;
using cse325_project.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Supabase.Gotrue.Exceptions;
using cse325_project.Services;


var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var supabaseSettings = builder.Configuration
    .GetSection("Supabase")
    .Get<SupabaseSettings>() ?? new SupabaseSettings();

builder.Services.AddSingleton(supabaseSettings);
builder.Services.AddScoped<ISupabaseService, SupabaseService>();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
builder.Services.AddScoped<SupabaseAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<SupabaseAuthStateProvider>());
builder.Services.AddScoped<ShoppingListService>();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });
builder.Services.AddHttpContextAccessor();

// Keep Inventory Service too
builder.Services.AddSingleton<InventoryService>();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/login", async (
    HttpContext httpContext,
    ISupabaseService supabaseService,
    IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(httpContext);
    }
    catch (AntiforgeryValidationException ex)
    {
        return Results.BadRequest("Invalid request.");
    }

    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var returnUrl = SanitizeReturnUrl(form["returnUrl"].ToString());

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=missing");
    }

    try
    {
        await supabaseService.InitializeAsync();
        var result = await supabaseService.Client.Auth.SignIn(email, password);
        var (accessToken, refreshToken, userId, userEmail) = ExtractSessionTokens(result, supabaseService.Client.Auth.CurrentSession);

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Redirect("/login?error=confirm");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId ?? string.Empty),
            new Claim(ClaimTypes.Email, userEmail ?? email),
            new Claim(ClaimTypes.UserData, accessToken),
            new Claim("supabase:refresh_token", refreshToken)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });

        return Results.Redirect(returnUrl);
    }
    catch (GotrueException gte)
    {
        var error = gte.Reason switch
        {
            FailureHint.Reason.UserEmailNotConfirmed => "confirm",
            FailureHint.Reason.UserBadEmailAddress => "invalid",
            FailureHint.Reason.UserBadMultiple or FailureHint.Reason.UserBadLogin or FailureHint.Reason.UserBadPassword => "invalid",
            FailureHint.Reason.UserTooManyRequests => "rate",
            _ => "unknown"
        };

        return Results.Redirect($"/login?error={error}");
    }
    catch (Exception)
    {
        return Results.Redirect("/login?error=unknown");
    }
});

app.MapPost("/auth/logout", async (
    HttpContext httpContext,
    ISupabaseService supabaseService,
    IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(httpContext);
    }
    catch (AntiforgeryValidationException ex)
    {
        return Results.BadRequest("Invalid request.");
    }

    try
    {
        await supabaseService.InitializeAsync();
        await supabaseService.Client.Auth.SignOut();
    }
    catch (Exception)
    {
    }

    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string SanitizeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl))
    {
        return "/";
    }

    return Uri.TryCreate(returnUrl, UriKind.Relative, out _) ? returnUrl : "/";
}

static (string? AccessToken, string? RefreshToken, string? UserId, string? Email) ExtractSessionTokens(object? result, Supabase.Gotrue.Session? currentSession)
{
    var session = currentSession ?? result as Supabase.Gotrue.Session;
    if (session is null && result is not null)
    {
        var sessionProp = result.GetType().GetProperty("Session");
        session = sessionProp?.GetValue(result) as Supabase.Gotrue.Session;
    }

    if (session is null)
    {
        return (null, null, null, null);
    }

    var user = session.User;
    return (session.AccessToken, session.RefreshToken, user?.Id, user?.Email);
}

