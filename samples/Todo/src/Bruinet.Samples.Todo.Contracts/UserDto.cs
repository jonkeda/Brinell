using System.Text.Json.Serialization;

namespace Bruinet.Samples.Todo.Contracts;

/// <summary>
/// A user as it travels between the app and the API.
/// </summary>
/// <remarks>
/// Users are managed by the server: the app sends a change and caches the answer, and has no
/// offline edits to merge. The id is chosen by the client, as for todos, so a retried <c>PUT</c>
/// cannot create a duplicate.
/// </remarks>
/// <param name="Id">The client-chosen id.</param>
/// <param name="Name">Required, at most 50 characters, unique among users that are not inactive.</param>
/// <param name="Email">An email address, or <c>null</c>.</param>
/// <param name="Status">Active, on leave or inactive.</param>
/// <param name="AllowedUntil">The last day the user may work, or <c>null</c> for no end.</param>
/// <param name="Role">What the user is; recorded only, it grants nothing.</param>
/// <param name="HoursPerWeek">1 to 60.</param>
/// <param name="HourlyRate">0 to 10,000.00, at most two decimals.</param>
/// <param name="StartOfDay">When the working day starts.</param>
/// <param name="ReceivesReminders">Whether the user gets reminders.</param>
/// <param name="Workload">0 to 100, in steps of 5.</param>
/// <param name="Notes">Free text, at most 500 characters, or <c>null</c>.</param>
/// <param name="UpdatedAt">When it last changed, UTC.</param>
public sealed record UserDto(
    Guid Id,
    string Name,
    string? Email,
    UserStatusDto Status,
    DateOnly? AllowedUntil,
    UserRoleDto Role,
    int HoursPerWeek,
    decimal HourlyRate,
    TimeOnly StartOfDay,
    bool ReceivesReminders,
    int Workload,
    string? Notes,
    DateTimeOffset UpdatedAt);

/// <summary>A user's status on the wire, written as its name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserStatusDto>))]
public enum UserStatusDto
{
    /// <summary>Working.</summary>
    Active,

    /// <summary>Away for now.</summary>
    OnLeave,

    /// <summary>Gone; kept so their history stays readable.</summary>
    Inactive,
}

/// <summary>A user's role on the wire, written as its name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRoleDto>))]
public enum UserRoleDto
{
    /// <summary>A team member.</summary>
    Member,

    /// <summary>Leads a team.</summary>
    Lead,

    /// <summary>Administers the app.</summary>
    Admin,
}
