using System.Globalization;
using Bruinet.Samples.Todo.Core.Models;

namespace Bruinet.Samples.Todo.Core.Rules;

/// <summary>A user field that can be refused.</summary>
public enum UserField
{
    /// <summary>The name.</summary>
    Name,

    /// <summary>The email address.</summary>
    Email,

    /// <summary>The hours per week.</summary>
    HoursPerWeek,

    /// <summary>The hourly rate.</summary>
    HourlyRate,

    /// <summary>The workload.</summary>
    Workload,

    /// <summary>The notes.</summary>
    Notes,
}

/// <summary>Why one field of a user cannot be saved.</summary>
/// <param name="Field">The field.</param>
/// <param name="Message">What to show on it.</param>
public sealed record UserFieldError(UserField Field, string Message);

/// <summary>
/// What a user must satisfy before it can be saved. The server checks the same rules and answers
/// with the same messages.
/// </summary>
public static partial class UserValidation
{
    /// <summary>The longest name allowed.</summary>
    public const int MaxNameLength = 50;

    /// <summary>The fewest hours per week.</summary>
    public const int MinHoursPerWeek = 1;

    /// <summary>The most hours per week.</summary>
    public const int MaxHoursPerWeek = 60;

    /// <summary>The highest hourly rate.</summary>
    public const decimal MaxHourlyRate = 10_000m;

    /// <summary>The workload moves in steps of this many percent.</summary>
    public const int WorkloadStep = 5;

    /// <summary>The longest notes allowed.</summary>
    public const int MaxNotesLength = 500;

    /// <summary>Shown when the name is empty.</summary>
    public const string NameRequired = "Name is required";

    /// <summary>Shown when the name is too long.</summary>
    public const string NameTooLong = "Name can be at most 50 characters";

    /// <summary>Shown when another user that is not inactive has the name.</summary>
    public const string NameTaken = "Another user already has this name";

    /// <summary>Shown when the email address is not one.</summary>
    public const string EmailInvalid = "Enter an email address such as eva@example.com";

    /// <summary>Shown when the hours are out of range.</summary>
    public const string HoursOutOfRange = "Hours per week must be between 1 and 60";

    /// <summary>Shown when the rate cannot be read.</summary>
    public const string RateUnreadable = "Enter a rate such as 1,250.75";

    /// <summary>Shown when the rate is out of range.</summary>
    public const string RateOutOfRange = "Hourly rate must be between 0 and 10,000.00";

    /// <summary>Shown when the rate has more than two decimals.</summary>
    public const string RateTooPrecise = "Hourly rate can have at most two decimals";

    /// <summary>Shown when the workload is out of range or between steps.</summary>
    public const string WorkloadInvalid = "Workload must be 0 to 100, in steps of 5";

    /// <summary>Shown when the notes are too long.</summary>
    public const string NotesTooLong = "Notes can be at most 500 characters";

    /// <summary>
    /// Reads a rate as typed: <c>1,250.75</c>, with <c>.</c> for decimals and <c>,</c> between
    /// thousands, whatever the device's culture (AD-016's number format).
    /// </summary>
    /// <returns>The error, or <c>null</c> with <paramref name="rate"/> set.</returns>
    public static string? ParseRate(string? text, out decimal rate)
    {
        rate = 0;
        var trimmed = text?.Trim() ?? string.Empty;

        // Groups of exactly three after a comma, or no commas at all: "1,5" is not 15.
        if (!RateFormat().IsMatch(trimmed)
            || !decimal.TryParse(trimmed, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out rate))
        {
            return RateUnreadable;
        }

        return null;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^-?(\d{1,3}(,\d{3})+|\d+)(\.\d+)?$")]
    private static partial System.Text.RegularExpressions.Regex RateFormat();

    /// <summary>A rate as the form shows it: <c>1,250.75</c>.</summary>
    public static string FormatRate(decimal rate) => rate.ToString("#,##0.00", CultureInfo.InvariantCulture);

    /// <summary>
    /// Every problem with <paramref name="user"/>, one per field; empty when it can be saved.
    /// </summary>
    /// <param name="user">The user as it would be saved.</param>
    /// <param name="others">The users already known, to check the name is free. The user itself is skipped by id.</param>
    public static IReadOnlyList<UserFieldError> Validate(User user, IEnumerable<User> others)
    {
        var errors = new List<UserFieldError>();

        var name = user.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            errors.Add(new(UserField.Name, NameRequired));
        }
        else if (name.Length > MaxNameLength)
        {
            errors.Add(new(UserField.Name, NameTooLong));
        }
        else if (user.Status != UserStatus.Inactive && others.Any(other => other.Id != user.Id
            && other.Status != UserStatus.Inactive
            && string.Equals(other.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(new(UserField.Name, NameTaken));
        }

        if (!string.IsNullOrWhiteSpace(user.Email) && !IsEmail(user.Email.Trim()))
        {
            errors.Add(new(UserField.Email, EmailInvalid));
        }

        if (user.HoursPerWeek is < MinHoursPerWeek or > MaxHoursPerWeek)
        {
            errors.Add(new(UserField.HoursPerWeek, HoursOutOfRange));
        }

        if (user.HourlyRate is < 0 or > MaxHourlyRate)
        {
            errors.Add(new(UserField.HourlyRate, RateOutOfRange));
        }
        else if (decimal.Round(user.HourlyRate, 2) != user.HourlyRate)
        {
            errors.Add(new(UserField.HourlyRate, RateTooPrecise));
        }

        if (user.Workload is < 0 or > 100 || user.Workload % WorkloadStep != 0)
        {
            errors.Add(new(UserField.Workload, WorkloadInvalid));
        }

        if (user.Notes is { } notes && notes.Trim().Length > MaxNotesLength)
        {
            errors.Add(new(UserField.Notes, NotesTooLong));
        }

        return errors;
    }

    /// <summary>One <c>@</c> with text before it, and a dot in the part after it.</summary>
    public static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');
        return at > 0
            && at == value.LastIndexOf('@')
            && value.IndexOf('.', at) > at + 1
            && !value.EndsWith('.')
            && !value.Any(char.IsWhiteSpace);
    }
}
