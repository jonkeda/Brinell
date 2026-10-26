namespace Bruinet.Samples.Todo.Core.Models;

/// <summary>
/// A user of the Todo app. Users are managed by the server; the app keeps a cached copy.
/// </summary>
/// <remarks>
/// Not linked to todos yet. The fields are chosen so the edit page has one field of each kind a
/// test drives: text, email, a choice of three, an optional date, a fixed list, a whole number, a
/// decimal, a time, on/off, a range and multi-line text.
/// </remarks>
public sealed record User
{
    /// <summary>Chosen by the client when the user is created, and kept on the server.</summary>
    public required Guid Id { get; init; }

    /// <summary>Required, at most <see cref="Rules.UserValidation.MaxNameLength"/> characters.</summary>
    public required string Name { get; init; }

    /// <summary>An email address, or <c>null</c>.</summary>
    public string? Email { get; init; }

    /// <summary>Active, on leave or inactive.</summary>
    public UserStatus Status { get; init; }

    /// <summary>The last day the user may work, or <c>null</c> for no end. May be in the past.</summary>
    public DateOnly? AllowedUntil { get; init; }

    /// <summary>What the user is; recorded only, it grants nothing.</summary>
    public UserRole Role { get; init; }

    /// <summary>1 to 60.</summary>
    public int HoursPerWeek { get; init; } = 40;

    /// <summary>0 to 10,000.00, at most two decimals.</summary>
    public decimal HourlyRate { get; init; }

    /// <summary>When the working day starts.</summary>
    public TimeOnly StartOfDay { get; init; } = new(9, 0);

    /// <summary>Whether the user gets reminders.</summary>
    public bool ReceivesReminders { get; init; } = true;

    /// <summary>0 to 100, in steps of 5.</summary>
    public int Workload { get; init; }

    /// <summary>Free text, or <c>null</c>.</summary>
    public string? Notes { get; init; }

    /// <summary>When it last changed, UTC.</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>Whether a user is working.</summary>
public enum UserStatus
{
    /// <summary>Working.</summary>
    Active,

    /// <summary>Away for now.</summary>
    OnLeave,

    /// <summary>Gone. Never deleted, so their history stays readable.</summary>
    Inactive,
}

/// <summary>What a user is. Recorded only: no role grants anything.</summary>
public enum UserRole
{
    /// <summary>A team member.</summary>
    Member,

    /// <summary>Leads a team.</summary>
    Lead,

    /// <summary>Administers the app.</summary>
    Admin,
}
