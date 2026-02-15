namespace cse325_project.lib;

public static class TextHelpers
{
    public static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    public static string ResolveDisplayName(string? preferredDisplayName, string? existingDisplayName, string email)
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

        return DisplayNameFromEmail(email);
    }

    public static string DisplayNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return "User";
        }

        var at = email.IndexOf('@');
        var localPart = at > 0 ? email[..at] : email;
        var tokens = localPart
            .Replace('.', ' ')
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return "User";
        }

        return NormalizeDisplayName(string.Join(' ', tokens));
    }

    public static string NormalizeDisplayName(string? value)
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
