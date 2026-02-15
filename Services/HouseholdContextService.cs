using cse325_project.Models.Database;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace cse325_project.Services;

public interface IHouseholdContextService
{
    Task<(AppUser User, Group Group)> EnsureForClaimsAsync(string? userIdClaim, string? emailClaim, string? preferredDisplayName = null);
    Task<(AppUser User, Group Group)> EnsureForPrincipalAsync(ClaimsPrincipal principal, string? preferredDisplayName = null);
}

public sealed class HouseholdContextService : IHouseholdContextService
{
    private readonly ISupabaseService _supabaseService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HouseholdContextService(
        ISupabaseService supabaseService,
        IHttpContextAccessor httpContextAccessor)
    {
        _supabaseService = supabaseService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<(AppUser User, Group Group)> EnsureForPrincipalAsync(ClaimsPrincipal principal, string? preferredDisplayName = null)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
        return EnsureForClaimsAsync(userIdClaim, emailClaim, preferredDisplayName);
    }

    public async Task<(AppUser User, Group Group)> EnsureForClaimsAsync(string? userIdClaim, string? emailClaim, string? preferredDisplayName = null)
    {
        await _supabaseService.InitializeAsync();
        await EnsureSessionFromHttpContextAsync(userIdClaim);

        var effectiveUserId = userIdClaim;
        if (string.IsNullOrWhiteSpace(effectiveUserId))
        {
            effectiveUserId = _supabaseService.Client.Auth.CurrentUser?.Id
                ?? _supabaseService.Client.Auth.CurrentSession?.User?.Id;
        }

        if (!Guid.TryParse(effectiveUserId, out var userId))
        {
            throw new InvalidOperationException("Authenticated user id claim is missing or invalid.");
        }

        var userResponse = await _supabaseService.Client
            .From<AppUser>()
            .Where(u => u.UserId == userId)
            .Limit(1)
            .Get();

        var user = userResponse.Models?.FirstOrDefault();
        var resolvedEmail = (emailClaim ?? _supabaseService.Client.Auth.CurrentUser?.Email ?? user?.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(resolvedEmail))
        {
            throw new InvalidOperationException("Authenticated email claim is missing.");
        }

        var resolvedDisplayName = ResolveDisplayName(preferredDisplayName, user?.DisplayName, resolvedEmail);

        if (user is null)
        {
            var newUser = new AppUser
            {
                UserId = userId,
                Email = resolvedEmail,
                DisplayName = resolvedDisplayName,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _supabaseService.Client
                    .From<AppUser>()
                    .Insert(newUser);
                user = newUser;
            }
            catch
            {
                // Auth trigger may have inserted the profile concurrently; reload once before failing.
                var retryUserResponse = await _supabaseService.Client
                    .From<AppUser>()
                    .Where(u => u.UserId == userId)
                    .Limit(1)
                    .Get();

                user = retryUserResponse.Models?.FirstOrDefault();
                if (user is null)
                {
                    throw;
                }
            }
        }
        else if (!string.Equals(user.Email, resolvedEmail, StringComparison.OrdinalIgnoreCase) ||
                 !string.Equals(user.DisplayName, resolvedDisplayName, StringComparison.Ordinal))
        {
            await _supabaseService.Client
                .From<AppUser>()
                .Where(u => u.UserId == user.UserId)
                .Set(u => u.Email!, resolvedEmail)
                .Set(u => u.DisplayName!, resolvedDisplayName)
                .Update();

            user.Email = resolvedEmail;
            user.DisplayName = resolvedDisplayName;
        }

        var membershipResponse = await _supabaseService.Client
            .From<GroupMember>()
            .Where(m => m.UserId == userId)
            .Limit(1)
            .Get();

        var membership = membershipResponse.Models?.FirstOrDefault();
        Group? group = null;

        if (membership is not null)
        {
            var groupResponse = await _supabaseService.Client
                .From<Group>()
                .Where(g => g.GroupId == membership.GroupId)
                .Limit(1)
                .Get();
            group = groupResponse.Models?.FirstOrDefault();
        }

        if (group is null)
        {
            group = new Group
            {
                GroupId = Guid.NewGuid(),
                Name = $"{resolvedDisplayName}'s Household",
                CreatedByUser = userId,
                CreatedAt = DateTime.UtcNow
            };

            await _supabaseService.Client
                .From<Group>()
                .Insert(group);

            await _supabaseService.Client
                .From<GroupMember>()
                .Insert(new GroupMember
                {
                    GroupId = group.GroupId,
                    UserId = userId,
                    Role = "owner",
                    JoinedAt = DateTime.UtcNow
                });
        }

        return (user, group);
    }

    private async Task EnsureSessionFromHttpContextAsync(string? userIdClaim)
    {
        var sessionUserId = _supabaseService.Client.Auth.CurrentSession?.User?.Id
            ?? _supabaseService.Client.Auth.CurrentUser?.Id;

        if (!string.IsNullOrWhiteSpace(sessionUserId) &&
            (string.IsNullOrWhiteSpace(userIdClaim) || string.Equals(sessionUserId, userIdClaim, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var principal = _httpContextAccessor.HttpContext?.User;
        var accessToken = principal?.FindFirst(ClaimTypes.UserData)?.Value;
        var refreshToken = principal?.FindFirst("supabase:refresh_token")?.Value;

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        try
        {
            await _supabaseService.Client.Auth.SetSession(accessToken, refreshToken, true);
        }
        catch
        {
        }
    }

    private static string ResolveDisplayName(string? preferredDisplayName, string? existingDisplayName, string email)
    {
        var preferred = NormalizeDisplayName(preferredDisplayName);
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        var existing = NormalizeDisplayName(existingDisplayName);
        if (!string.IsNullOrWhiteSpace(existing))
        {
            return existing;
        }

        var atIndex = email.IndexOf('@');
        var localPart = atIndex > 0 ? email[..atIndex] : email;
        var tokens = localPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length > 0)
        {
            return NormalizeDisplayName(string.Join(' ', tokens));
        }

        return "User";
    }

    private static string NormalizeDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value
            .Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToNameCase);

        return string.Join(' ', parts);
    }

    private static string ToNameCase(string part)
    {
        if (string.IsNullOrWhiteSpace(part))
        {
            return string.Empty;
        }

        if (part.Length == 1)
        {
            return part.ToUpperInvariant();
        }

        return char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
    }
}
