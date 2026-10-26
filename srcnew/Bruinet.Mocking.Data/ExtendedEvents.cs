using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Bruinet.Mocking.Data;

/// <summary>What <see cref="ExtendedEvents.StartAsync"/> leaves for <see cref="ExtendedEvents.ReadAsync"/>: the session and its marker.</summary>
public sealed class CaptureState
{
    public string Session { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;

    /// <summary>The session's files on the server, as <c>sys.fn_xe_file_target_read_file</c> takes them.</summary>
    public string Files { get; set; } = string.Empty;

    /// <summary>The marker query's text.</summary>
    public string Marker { get; set; } = string.Empty;

    /// <summary>When the marker was sent, on this machine's clock, UTC.</summary>
    public DateTime MarkerSent { get; set; }

    /// <summary>When the marker came back, on this machine's clock, UTC.</summary>
    public DateTime MarkerDone { get; set; }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));

    public static CaptureState Load(string path) => JsonSerializer.Deserialize<CaptureState>(File.ReadAllText(path)) ?? new CaptureState();
}

/// <summary>
/// The SQL log from SQL Server itself, for an app that is left unchanged: an Extended Events session on one database
/// records each RPC and batch with its duration, row count and text, and is read back as <see cref="SqlLogEntry"/>s. Needs
/// <c>ALTER ANY EVENT SESSION</c> and a folder the server can write to. The entries name no test: the report gives them
/// to tests by time. A marker query sent at the start lines the server's clock up with this machine's.
/// </summary>
public static class ExtendedEvents
{
    private static readonly Regex Unsafe = new(@"[^A-Za-z0-9_]", RegexOptions.Compiled);
    private static readonly Regex NamedParameter = new(@"@\w+", RegexOptions.Compiled);

    /// <summary>The session's name for <paramref name="database"/>.</summary>
    public static string SessionName(string database) => "bruinet_sql_" + Unsafe.Replace(database, "_");

    /// <summary>
    /// Creates and starts the session on the database <paramref name="connection"/> is open on (an older one of the same
    /// name dropped first), its files in <paramref name="folder"/> on the server, and sends the marker.
    /// </summary>
    /// <remarks>
    /// <c>like</c>, a <c>LIKE</c> pattern such as <c>Suite_%</c>, captures every database it matches instead of the
    /// connection's own, for a suite that makes a database per run; the session is named for the pattern.
    /// </remarks>
    public static async Task<CaptureState> StartAsync(DbConnection connection, string folder, CancellationToken cancellationToken = default, string? like = null)
    {
        var database = like ?? connection.Database;
        var session = SessionName(database);
        // A name of its own per capture: an earlier capture's files stay on the server, and must not be read with this one.
        var files = folder.TrimEnd('\\', '/') + "\\" + session + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var file = files + ".xel";
        var filter = like is null
            ? $"sqlserver.database_name = N'{database.Replace("'", "''")}'"
            : $"sqlserver.like_i_sql_unicode_string(sqlserver.database_name, N'{like.Replace("'", "''")}')";
        await ExecuteAsync(connection, $"""
            IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = N'{session}') DROP EVENT SESSION [{session}] ON SERVER;
            CREATE EVENT SESSION [{session}] ON SERVER
            ADD EVENT sqlserver.rpc_completed (ACTION (sqlserver.database_name) WHERE ({filter})),
            ADD EVENT sqlserver.sql_batch_completed (ACTION (sqlserver.database_name) WHERE ({filter}) OR (sqlserver.like_i_sql_unicode_string(sqlserver.sql_text, N'%bruinet-marker-%')))
            ADD TARGET package0.event_file (SET filename = N'{file.Replace("'", "''")}', max_file_size = 100)
            WITH (MAX_DISPATCH_LATENCY = 1 SECONDS, EVENT_RETENTION_MODE = ALLOW_SINGLE_EVENT_LOSS);
            ALTER EVENT SESSION [{session}] ON SERVER STATE = START;
            """, cancellationToken).ConfigureAwait(false);

        var marker = $"SELECT 'bruinet-marker-{Guid.NewGuid():N}'";
        var sent = DateTime.UtcNow;
        await ExecuteAsync(connection, marker, cancellationToken).ConfigureAwait(false);
        return new CaptureState
        {
            Session = session,
            Database = database,
            Files = files + "*.xel",
            Marker = marker,
            MarkerSent = sent,
            MarkerDone = DateTime.UtcNow,
        };
    }

    /// <summary>Stops and drops the session; its files stay for <see cref="ReadAsync"/>.</summary>
    public static Task StopAsync(DbConnection connection, string database, CancellationToken cancellationToken = default)
    {
        var session = SessionName(database);
        return ExecuteAsync(connection, $"""
            IF EXISTS (SELECT 1 FROM sys.dm_xe_sessions WHERE name = N'{session}') ALTER EVENT SESSION [{session}] ON SERVER STATE = STOP;
            IF EXISTS (SELECT 1 FROM sys.server_event_sessions WHERE name = N'{session}') DROP EVENT SESSION [{session}] ON SERVER;
            """, cancellationToken);
    }

    /// <summary>The session's events as log entries, on this machine's clock, the marker left out.</summary>
    public static async Task<IReadOnlyList<SqlLogEntry>> ReadAsync(DbConnection connection, CaptureState state, CancellationToken cancellationToken = default)
    {
        var events = new List<XElement>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT CAST(event_data AS nvarchar(max)) FROM sys.fn_xe_file_target_read_file(@files, NULL, NULL, NULL)";
            var files = command.CreateParameter();
            files.ParameterName = "@files";
            files.Value = state.Files;
            command.Parameters.Add(files);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                events.Add(XElement.Parse(reader.GetString(0)));
            }
        }

        // The marker's start on the server's clock against when it was sent from here: a command's start is then when
        // this machine sent it, as the timings and an in-process log see it, a few milliseconds before the server began.
        var marker = events.FirstOrDefault(element => Text(element) == state.Marker);
        var offset = marker is null ? TimeSpan.Zero : Timestamp(marker) - Duration(marker) - state.MarkerSent;
        return events
            .Where(element => element != marker)
            .Select(element => Entry(element, offset))
            .OfType<SqlLogEntry>()
            .OrderBy(entry => entry.Start)
            .ToList();
    }

    /// <summary>One event as an entry: an <c>sp_executesql</c> call as its inner statement, a procedure call as its <c>EXEC</c>.</summary>
    public static SqlLogEntry? Entry(XElement element, TimeSpan offset)
    {
        var text = Text(element);
        if (text is null)
        {
            return null;
        }

        var duration = Duration(element);
        var start = Timestamp(element) - offset - duration;
        var parameters = element.Attribute("name")?.Value == "rpc_completed" ? Parameters(element) : null;
        var entry = SqlLog.Entry(Kind(text), text, parameters, start, duration);
        if (Data(element, "row_count") is { } rows && long.TryParse(rows, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
        {
            if (Statements.IsWrite(text))
            {
                entry.Affected = (int)count;
            }
            else
            {
                entry.Rows = (int)count;
            }
        }

        if (DataText(element, "result") is { } result && !string.Equals(result, "OK", StringComparison.OrdinalIgnoreCase))
        {
            entry.Kind = "error";
        }

        return entry;
    }

    private static string Kind(string text) => Statements.IsWrite(text) ? "non-query" : "reader";

    // An RPC's statement: sp_executesql's first argument, or a procedure's EXEC; a batch's text as sent.
    private static string? Text(XElement element)
    {
        if (element.Attribute("name")?.Value == "sql_batch_completed")
        {
            return Data(element, "batch_text");
        }

        var name = Data(element, "object_name");
        var statement = Data(element, "statement");
        if (string.Equals(name, "sp_executesql", StringComparison.OrdinalIgnoreCase) && statement is not null)
        {
            return Literal(statement, 0);
        }

        // SqlClient resetting a pooled connection: the driver's housekeeping, not a command the app sent.
        if (string.Equals(name, "sp_reset_connection", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return name is null ? statement : "EXEC " + name;
    }

    // sp_executesql's parameter list (its second argument) or the procedure call's @names.
    private static IEnumerable<string> Parameters(XElement element)
    {
        var statement = Data(element, "statement") ?? string.Empty;
        var declared = string.Equals(Data(element, "object_name"), "sp_executesql", StringComparison.OrdinalIgnoreCase) ? Literal(statement, 1) : statement;
        return NamedParameter.Matches(declared ?? string.Empty).Cast<Match>().Select(match => match.Value).Distinct();
    }

    // The n-th N'...' literal in an EXEC statement, its quotes undone.
    private static string? Literal(string statement, int n)
    {
        var index = 0;
        for (var i = 0; i < statement.Length; i++)
        {
            if (statement[i] != '\'')
            {
                continue;
            }

            var value = new System.Text.StringBuilder();
            var j = i + 1;
            for (; j < statement.Length; j++)
            {
                if (statement[j] == '\'')
                {
                    if (j + 1 < statement.Length && statement[j + 1] == '\'')
                    {
                        value.Append('\'');
                        j++;
                        continue;
                    }

                    break;
                }

                value.Append(statement[j]);
            }

            if (index++ == n)
            {
                return value.ToString();
            }

            i = j;
        }

        return null;
    }

    // XE's duration is in microseconds.
    private static TimeSpan Duration(XElement element)
        => TimeSpan.FromTicks(long.Parse(Data(element, "duration") ?? "0", CultureInfo.InvariantCulture) * 10);

    private static DateTime Timestamp(XElement element)
        => DateTime.Parse(element.Attribute("timestamp")!.Value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    private static string? Data(XElement element, string name)
        => element.Elements("data").FirstOrDefault(data => data.Attribute("name")?.Value == name)?.Element("value")?.Value;

    private static string? DataText(XElement element, string name)
        => element.Elements("data").FirstOrDefault(data => data.Attribute("name")?.Value == name)?.Element("text")?.Value;

    private static async Task ExecuteAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
