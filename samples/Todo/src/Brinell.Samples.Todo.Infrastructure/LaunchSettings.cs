using System.Globalization;
using Brinell.Samples.Todo.Contracts;

namespace Brinell.Samples.Todo.Infrastructure;

/// <summary>
/// Everything about the app that a test run may change, in one place.
/// </summary>
/// <remarks>
/// <para>
/// Read once at startup. On Windows the values come from environment variables the app inherits
/// from the test process; on Android they will come from intent extras. A normal launch sets none
/// of them and gets <see cref="From"/>'s defaults.
/// </para>
/// <para>
/// This replaces a compile-time switch (Construction's <c>#if WINDOWS</c> around
/// <c>CONSTRUCTION_USE_MOCK_BACKEND</c>) with one object that works on every head and that the
/// integration tests build directly.
/// </para>
/// </remarks>
public sealed record LaunchSettings
{
    /// <summary>The SQLite file.</summary>
    public const string DatabasePathVariable = "TODO_DB_PATH";

    /// <summary>The backend's base URL.</summary>
    public const string ApiBaseUrlVariable = "TODO_API_BASEURL";

    /// <summary>The backend's API key.</summary>
    public const string ApiKeyVariable = "TODO_API_KEY";

    /// <summary>A fixed "now", round-trip format, so Overdue and timestamps are deterministic.</summary>
    public const string FixedNowVariable = "TODO_NOW";

    /// <summary><c>true</c> or <c>false</c>: sync when the list first appears.</summary>
    public const string SyncOnStartVariable = "TODO_SYNC_ON_START";

    /// <summary>A network-state file that can take the app offline.</summary>
    public const string NetworkStateFileVariable = "TODO_NETWORK_STATE_FILE";

    /// <summary>The API timeout in seconds.</summary>
    public const string ApiTimeoutVariable = "TODO_API_TIMEOUT_SECONDS";

    /// <summary>The SQLite file.</summary>
    public required string DatabasePath { get; init; }

    /// <summary>The backend's base URL.</summary>
    public string ApiBaseUrl { get; init; } = TodoApi.DevelopmentBaseUrl;

    /// <summary>The backend's API key.</summary>
    public string ApiKey { get; init; } = TodoApi.DevelopmentApiKey;

    /// <summary>A fixed "now"; <c>null</c> for the real clock.</summary>
    public DateTimeOffset? FixedNow { get; init; }

    /// <summary>Whether the list syncs the first time it appears.</summary>
    public bool SyncOnStart { get; init; } = true;

    /// <summary>A network-state file; <c>null</c> for the device's own connectivity only.</summary>
    public string? NetworkStateFile { get; init; }

    /// <summary>How long an API call may take.</summary>
    public TimeSpan ApiTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Builds settings from named values (environment variables, intent extras), falling back to
    /// the defaults for anything not set.
    /// </summary>
    /// <param name="read">Reads a named value; <c>null</c> or empty means "not set".</param>
    /// <param name="defaultDatabasePath">The app's normal database file.</param>
    /// <exception cref="FormatException">A value is set but cannot be read; the message names it.</exception>
    public static LaunchSettings From(Func<string, string?> read, string defaultDatabasePath)
    {
        ArgumentNullException.ThrowIfNull(read);

        string? Value(string name) => read(name) is { Length: > 0 } value ? value.Trim() : null;

        return new LaunchSettings
        {
            DatabasePath = Value(DatabasePathVariable) ?? defaultDatabasePath,
            ApiBaseUrl = Value(ApiBaseUrlVariable) ?? TodoApi.DevelopmentBaseUrl,
            ApiKey = Value(ApiKeyVariable) ?? TodoApi.DevelopmentApiKey,
            FixedNow = Value(FixedNowVariable) is { } now ? Parse(FixedNowVariable, now, ParseTime) : null,
            SyncOnStart = Value(SyncOnStartVariable) is not { } sync || Parse(SyncOnStartVariable, sync, bool.Parse),
            NetworkStateFile = Value(NetworkStateFileVariable),
            ApiTimeout = Value(ApiTimeoutVariable) is { } seconds
                ? TimeSpan.FromSeconds(Parse(ApiTimeoutVariable, seconds, text => double.Parse(text, CultureInfo.InvariantCulture)))
                : TimeSpan.FromSeconds(10),
        };
    }

    /// <summary>The settings as named values, the inverse of <see cref="From"/>. What a test fixture hands the app.</summary>
    public IReadOnlyDictionary<string, string> ToVariables()
    {
        var variables = new Dictionary<string, string>
        {
            [DatabasePathVariable] = DatabasePath,
            [ApiBaseUrlVariable] = ApiBaseUrl,
            [ApiKeyVariable] = ApiKey,
            [SyncOnStartVariable] = SyncOnStart ? "true" : "false",
            [ApiTimeoutVariable] = ApiTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture),
        };

        if (FixedNow is { } now)
        {
            variables[FixedNowVariable] = now.ToString("O", CultureInfo.InvariantCulture);
        }

        if (NetworkStateFile is { } file)
        {
            variables[NetworkStateFileVariable] = file;
        }

        return variables;
    }

    private static DateTimeOffset ParseTime(string text)
        => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static T Parse<T>(string name, string text, Func<string, T> parse)
    {
        try
        {
            return parse(text);
        }
        catch (FormatException failure)
        {
            throw new FormatException($"{name} is set to '{text}', which cannot be read: {failure.Message}", failure);
        }
    }
}

/// <summary>A clock stopped at one instant, for deterministic dates under test.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now, TimeZoneInfo? zone = null) : TimeProvider
{
    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

    /// <inheritdoc />
    public override TimeZoneInfo LocalTimeZone => zone ?? TimeZoneInfo.Local;
}
