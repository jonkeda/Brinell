using Bruinell.Samples.Todo.Core.Models;

namespace Bruinell.Samples.Todo.Core.Rules;

/// <summary>
/// Whether a user counts, and how the list shows them.
/// </summary>
public static class UserRules
{
    /// <summary>Shown for an active user whose end date has passed.</summary>
    public const string Expired = "Expired";

    /// <summary>Shown for a user on leave.</summary>
    public const string OnLeave = "On leave";

    /// <summary>Shown for an inactive user.</summary>
    public const string Inactive = "Inactive";

    /// <summary>The role names, in the order of <see cref="UserRole"/>.</summary>
    public static IReadOnlyList<string> RoleNames { get; } = ["Member", "Lead", "Admin"];

    /// <summary>
    /// Whether <paramref name="user"/> is available on <paramref name="today"/>: active, and with no
    /// end date or one that is today or later.
    /// </summary>
    public static bool IsAvailable(User user, DateOnly today)
        => user.Status == UserStatus.Active && (user.AllowedUntil is null || user.AllowedUntil >= today);

    /// <summary>"Inactive", "On leave", "Expired", or empty for an available user.</summary>
    public static string StateText(User user, DateOnly today) => user.Status switch
    {
        UserStatus.Inactive => Inactive,
        UserStatus.OnLeave => OnLeave,
        _ when user.AllowedUntil < today => Expired,
        _ => string.Empty,
    };

    /// <summary>The list's order: available users first, then the others, each group by name.</summary>
    public static IReadOnlyList<User> Arrange(IEnumerable<User> users, DateOnly today)
        => users
            .OrderBy(user => IsAvailable(user, today) ? 0 : 1)
            .ThenBy(user => user.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
