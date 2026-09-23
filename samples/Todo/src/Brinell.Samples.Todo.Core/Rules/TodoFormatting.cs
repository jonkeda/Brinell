using System.Globalization;
using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Sync;

namespace Brinell.Samples.Todo.Core.Rules;

/// <summary>
/// The texts the pages show for dates and sync state.
/// </summary>
/// <remarks>
/// Dates use the device's culture by default (TOD.03.2); tests pass a fixed culture.
/// </remarks>
public static class TodoFormatting
{
    /// <summary>"Due 12/03/2026", or empty when there is no due date.</summary>
    public static string Due(DateOnly? dueDate, IFormatProvider? culture = null)
        => dueDate is { } due ? "Due " + due.ToString("d", culture ?? CultureInfo.CurrentCulture) : string.Empty;

    /// <summary>A date in the device's format, or <paramref name="none"/> when there is none.</summary>
    public static string Date(DateOnly? date, string none, IFormatProvider? culture = null)
        => date is { } value ? value.ToString("d", culture ?? CultureInfo.CurrentCulture) : none;

    /// <summary>A stored UTC time in the device's time zone and culture.</summary>
    public static string Timestamp(DateTimeOffset utc, TimeZoneInfo? zone = null, IFormatProvider? culture = null)
        => TimeZoneInfo.ConvertTime(utc, zone ?? TimeZoneInfo.Local).ToString("g", culture ?? CultureInfo.CurrentCulture);

    /// <summary>The detail page's sync card (TOD.03.3).</summary>
    public static string SyncState(SyncState state) => state switch
    {
        Models.SyncState.Synced => "Synced",
        Models.SyncState.Pending => "Waiting to sync",
        Models.SyncState.LocalOnly => "Local only",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };

    /// <summary>The list page's sync line.</summary>
    public static string SyncLine(
        bool isOnline,
        SyncResult? last,
        TimeZoneInfo? zone = null,
        IFormatProvider? culture = null)
    {
        if (!isOnline)
        {
            return "Offline";
        }

        if (last is null)
        {
            return "Not synced yet";
        }

        return last.Error switch
        {
            SyncError.None => "Synced " + TimeZoneInfo.ConvertTime(last.At, zone ?? TimeZoneInfo.Local)
                .ToString("t", culture ?? CultureInfo.CurrentCulture),
            SyncError.Offline => "Offline",
            SyncError.Unreachable => "Server unreachable",
            SyncError.Timeout => "Sync timed out",
            SyncError.Unauthorized => "Not authorised",
            SyncError.ServerError => "Sync failed",
            _ => throw new ArgumentOutOfRangeException(nameof(last), last.Error, null),
        };
    }
}
