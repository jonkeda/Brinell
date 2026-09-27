namespace Brinell.Samples.Todo.Contracts;

/// <summary>
/// What the server refuses in a user, field by field.
/// </summary>
/// <remarks>
/// Part of the contract: the real API and its WireMock fake both answer with these, so the fake
/// cannot drift from the server. The app checks the same rules before it sends
/// (<c>UserValidation</c> in Core), written separately because the app and the server share only
/// the wire format. The field keys are the JSON property names, so a client can put each message
/// on its field.
/// </remarks>
public static class ServerUserRules
{
    /// <summary>The problems with <paramref name="user"/>, by field; empty when there are none.</summary>
    public static Dictionary<string, string[]> Validate(UserDto user)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string field, string message) => errors[field] = [message];

        var name = user.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            Add("name", "Name is required");
        }
        else if (name.Length > 50)
        {
            Add("name", "Name can be at most 50 characters");
        }

        if (user.Email is { } email && !IsEmail(email))
        {
            Add("email", "Enter an email address such as eva@example.com");
        }

        if (user.HoursPerWeek is < 1 or > 60)
        {
            Add("hoursPerWeek", "Hours per week must be between 1 and 60");
        }

        if (user.HourlyRate is < 0 or > 10_000m)
        {
            Add("hourlyRate", "Hourly rate must be between 0 and 10,000.00");
        }
        else if (decimal.Round(user.HourlyRate, 2) != user.HourlyRate)
        {
            Add("hourlyRate", "Hourly rate can have at most two decimals");
        }

        if (user.Workload is < 0 or > 100 || user.Workload % 5 != 0)
        {
            Add("workload", "Workload must be 0 to 100, in steps of 5");
        }

        if (user.Notes is { Length: > 500 })
        {
            Add("notes", "Notes can be at most 500 characters");
        }

        return errors;
    }

    /// <summary>Whether <paramref name="user"/>'s name is taken by another user that is not inactive.</summary>
    public static bool IsDuplicateName(UserDto user, IEnumerable<UserDto> existing)
        => user.Status != UserStatusDto.Inactive
            && existing.Any(other => other.Id != user.Id
                && other.Status != UserStatusDto.Inactive
                && string.Equals(other.Name.Trim(), user.Name.Trim(), StringComparison.OrdinalIgnoreCase));

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0
            && at == value.LastIndexOf('@')
            && value.IndexOf('.', at) > at + 1
            && !value.EndsWith('.')
            && !value.Any(char.IsWhiteSpace);
    }
}
