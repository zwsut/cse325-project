using System.Security.Claims;
using cse325_project.lib;
using cse325_project.Models.Database;
using Microsoft.AspNetCore.Http;

namespace cse325_project.Services;

public interface IUserContextService
{
    Task<(AppUser User, Group? Group)> GetForClaimsAsync(
        string? userIdClaim,
        string? emailClaim,
        string? preferredDisplayName = null);
}

public sealed class UserContextService : IUserContextService
{
    private readonly ISupabaseService _supabaseService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextService(
        ISupabaseService supabaseService,
        IHttpContextAccessor httpContextAccessor)
    {
        _supabaseService = supabaseService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(AppUser User, Group? Group)> GetForClaimsAsync(
        string? userIdClaim,
        string? emailClaim,
        string? preferredDisplayName = null)
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

        var resolvedDisplayName = TextHelpers.ResolveDisplayName(preferredDisplayName, user?.DisplayName, resolvedEmail);

        if (user is null)
        {
            user = new AppUser
            {
                UserId = userId,
                Email = resolvedEmail,
                DisplayName = resolvedDisplayName
            };
        }
        else
        {
            user.Email = string.IsNullOrWhiteSpace(user.Email) ? resolvedEmail : user.Email;
            user.DisplayName = string.IsNullOrWhiteSpace(user.DisplayName) ? resolvedDisplayName : user.DisplayName;
        }

        Group? group = null;
        var membershipResponse = await _supabaseService.Client
            .From<GroupMember>()
            .Where(m => m.UserId == userId)
            .Limit(1)
            .Get();

        var membership = membershipResponse.Models?.FirstOrDefault();
        if (membership is not null)
        {
            var groupResponse = await _supabaseService.Client
                .From<Group>()
                .Where(g => g.GroupId == membership.GroupId)
                .Limit(1)
                .Get();
            group = groupResponse.Models?.FirstOrDefault();
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

}
