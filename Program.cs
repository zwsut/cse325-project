using System.Security.Claims;
using cse325_project.Components;
using cse325_project.lib;
using cse325_project.Models.Database;
using cse325_project.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;


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
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IAppDataChangeService, AppDataChangeService>();
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
    catch (AntiforgeryValidationException)
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

        await SignInWithCookieAsync(httpContext, accessToken, refreshToken, userId, userEmail ?? email);

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

app.MapPost("/auth/signup", async (
    HttpContext httpContext,
    ISupabaseService supabaseService,
    IAntiforgery antiforgery,
    ILogger<Program> logger) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(httpContext);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest("Invalid request.");
    }

    var form = await httpContext.Request.ReadFormAsync();
    var firstName = form["firstName"].ToString().Trim();
    var lastName = form["lastName"].ToString().Trim();
    var email = form["email"].ToString().Trim();
    var password = form["password"].ToString();
    var confirmPassword = form["confirmPassword"].ToString();
    var returnUrl = SanitizeReturnUrl(form["returnUrl"].ToString());

    if (string.IsNullOrWhiteSpace(firstName) ||
        string.IsNullOrWhiteSpace(lastName) ||
        string.IsNullOrWhiteSpace(email) ||
        string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/signup?error=missing");
    }

    if (password.Length < 6 || !string.Equals(password, confirmPassword, StringComparison.Ordinal))
    {
        return Results.Redirect("/signup?error=password");
    }

    try
    {
        await supabaseService.InitializeAsync();
        var displayName = BuildDisplayName(firstName, lastName);

        var signupResult = await supabaseService.Client.Auth.SignUp(email, password, new SignUpOptions
        {
            Data = new Dictionary<string, object>
            {
                ["display_name"] = displayName
            }
        });
        var (accessToken, refreshToken, userId, userEmail) =
            ExtractSessionTokens(signupResult, supabaseService.Client.Auth.CurrentSession);

        // If sign-up does not create a session directly, sign in immediately.
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            var signInResult = await supabaseService.Client.Auth.SignIn(email, password);
            (accessToken, refreshToken, userId, userEmail) =
                ExtractSessionTokens(signInResult, supabaseService.Client.Auth.CurrentSession);
        }

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Redirect("/signup?error=confirm");
        }

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return Results.Redirect("/signup?error=unknown");
        }

        try
        {
            await EnsureSignupUserAndHouseholdAsync(supabaseService, parsedUserId, userEmail ?? email, displayName, accessToken, refreshToken, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create user household for {Email}", userEmail ?? email);
            return Results.Redirect("/signup?error=household");
        }

        await SignInWithCookieAsync(httpContext, accessToken, refreshToken, userId, userEmail ?? email);
        return Results.Redirect(returnUrl);
    }
    catch (GotrueException gte)
    {
        var error = gte.Reason switch
        {
            FailureHint.Reason.UserAlreadyRegistered => "exists",
            FailureHint.Reason.UserBadEmailAddress => "invalid",
            FailureHint.Reason.UserTooManyRequests => "rate",
            FailureHint.Reason.UserEmailNotConfirmed => "confirm",
            _ => "unknown"
        };

        return Results.Redirect($"/signup?error={error}");
    }
    catch (Exception)
    {
        return Results.Redirect("/signup?error=unknown");
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
    catch (AntiforgeryValidationException)
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

static async Task SignInWithCookieAsync(
    HttpContext httpContext,
    string accessToken,
    string refreshToken,
    string? userId,
    string email)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, userId ?? string.Empty),
        new(ClaimTypes.Email, email),
        new(ClaimTypes.UserData, accessToken),
        new("supabase:refresh_token", refreshToken)
    };

    var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    await httpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = true });
}

static string BuildDisplayName(string firstName, string lastName)
{
    var normalized = TextHelpers.NormalizeDisplayName($"{firstName} {lastName}");
    if (string.IsNullOrWhiteSpace(normalized))
    {
        return "User";
    }

    return normalized;
}

static async Task EnsureSignupUserAndHouseholdAsync(
    ISupabaseService supabaseService,
    Guid userId,
    string email,
    string displayName,
    string accessToken,
    string refreshToken,
    ILogger logger)
{
    logger.LogInformation("Starting household creation for user {UserId} ({Email})", userId, email);

    try
    {
        // Set the session so RLS policies know who the authenticated user is
        await supabaseService.Client.Auth.SetSession(accessToken, refreshToken, true);
        logger.LogInformation("Session set successfully for user {UserId}", userId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to set session for user {UserId}", userId);
        throw;
    }

    // User profile should normally be created by DB trigger, but ensure it exists for signup flow.
    try
    {
        var userResponse = await supabaseService.Client
            .From<AppUser>()
            .Where(u => u.UserId == userId)
            .Limit(1)
            .Get();

        var appUser = userResponse.Models?.FirstOrDefault();
        logger.LogInformation("User profile lookup: exists={Exists}, recordCount={RecordCount}", appUser != null, userResponse.Models?.Count ?? 0);

        if (appUser is null)
        {
            try
            {
                await supabaseService.Client
                    .From<AppUser>()
                    .Insert(new AppUser
                    {
                        UserId = userId,
                        Email = email,
                        DisplayName = displayName,
                        CreatedAt = DateTime.UtcNow
                    });
                logger.LogInformation("User profile created successfully for {UserId}", userId);
            }
            catch (Exception ex)
            {
                // Trigger path may have inserted profile row concurrently; ignore and continue.
                logger.LogWarning(ex, "Failed to insert user profile for {UserId} - may have been created by trigger", userId);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during user profile check/creation for {UserId}", userId);
        throw;
    }

    // Check for existing group membership
    try
    {
        var membershipResponse = await supabaseService.Client
            .From<GroupMember>()
            .Where(m => m.UserId == userId)
            .Limit(1)
            .Get();

        logger.LogInformation("Membership check: count={MembershipCount}", membershipResponse.Models?.Count ?? 0);

        if ((membershipResponse.Models?.Count ?? 0) > 0)
        {
            logger.LogInformation("User {UserId} already has group membership, skipping group creation", userId);
            return;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error checking group membership for {UserId}", userId);
        throw;
    }

    // Create group
    try
    {
        logger.LogInformation("Attempting to create household for user {UserId} via function call", userId);
        logger.LogInformation("RPC Parameters: p_user_id={UserId}, p_display_name={DisplayName}", userId, displayName);

        // Call the database function that handles group and group_member creation with proper permissions
        var rpcResponse = await supabaseService.Client.Rpc(
            "ensure_signup_household",
            new { p_user_id = userId, p_display_name = displayName }
        );

        logger.LogInformation("RPC response received: {Response}", rpcResponse);
        logger.LogInformation("Household created successfully via function for user {UserId}", userId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to create household for user {UserId}. Exception type: {ExceptionType}, Message: {Message}", userId, ex.GetType().Name, ex.Message);
        throw;
    }

    logger.LogInformation("Household creation completed successfully for user {UserId}", userId);
}

