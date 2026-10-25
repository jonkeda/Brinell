using System.CommandLine;
using Bruinell.Cli.Selection;
using Bruinell.Mocking.Data;
using Bruinell.Mocking.Data.Replay;
using Microsoft.Data.SqlClient;

namespace Bruinell.Cli.Sql;

/// <summary>
/// <c>bruinell sql</c>: the SQL log of a suite's run (.my/sql-replay/plan.md, step 1). The schema snapshot, an Extended
/// Events capture for an app left unchanged, and the report: the database's share of each test and how far each table,
/// view and procedure reaches. Read-only on the database, but for the capture's own session.
/// </summary>
internal static class SqlCommands
{
    /// <summary>The capture's state, between start and read, when no other file is named.</summary>
    public const string StateFile = "sql-capture.json";

    public static Command Build(CliIo io) => new("sql", "The SQL log: the schema snapshot, an Extended Events capture, and the report of what each test spent in the database.")
    {
        SchemaCommand(io),
        StaleCommand(io),
        new Command("capture", "An Extended Events session on the test database: the app's commands, logged by SQL Server, the app unchanged.")
        {
            StartCommand(io),
            StopCommand(io),
            ReadCommand(io),
        },
        ReportCommand(io),
    };

    private static Command SchemaCommand(CliIo io)
    {
        var connection = Connection();
        var output = new Option<string>("--out") { Description = "The snapshot's file.", DefaultValueFactory = _ => "schema.json" };
        var references = new Option<string?>("--references") { Description = "A JSON file of references kept by hand, for dynamic SQL: { \"dbo.search\": [\"dbo.places\"] }." };
        var data = new Option<string[]>("--data") { Description = "A table of reference data whose rows are hashed with it (repeatable, or comma-separated).", AllowMultipleArgumentsPerToken = true };
        var defaultSchema = new Option<string>("--default-schema") { Description = "The app's user's default schema: a command's unqualified name is taken in it, then dbo.", DefaultValueFactory = _ => "dbo" };
        var command = new Command("schema", "Write the database's schema snapshot: tables, views, procedures and functions, their hashes and references.") { connection, output, references, data, defaultSchema };
        command.Action(io, async (parsed, cancellationToken) =>
        {
            await using var sql = await OpenAsync(parsed.GetValue(connection)!, cancellationToken);
            var options = new SnapshotOptions
            {
                Data = [.. (parsed.GetValue(data) ?? []).SelectMany(item => item.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))],
                DefaultSchema = parsed.GetValue(defaultSchema)!,
            };
            var snapshot = await SchemaSnapshot.ReadAsync(sql, options, cancellationToken);
            if (parsed.GetValue(references) is { } file)
            {
                foreach (var unknown in snapshot.MergeReferences(Path.GetFullPath(file, io.CurrentDirectory)))
                {
                    io.Error.WriteLine($"warning: {unknown} in {file} is not in the database");
                }
            }

            var to = Path.GetFullPath(parsed.GetValue(output)!, io.CurrentDirectory);
            snapshot.Save(to);
            var dynamic = snapshot.Objects.Where(pair => pair.Value.Dynamic && !pair.Value.Listed).Select(pair => pair.Key).ToList();
            io.Error.WriteLine($"{snapshot.Objects.Count} objects of {sql.Database}; wrote {to}");
            if (dynamic.Count > 0)
            {
                io.Error.WriteLine($"{dynamic.Count} with dynamic SQL and no references listed (--references): {string.Join(", ", dynamic)}");
            }

            return 0;
        });
        return command;
    }

    private static Command StaleCommand(CliIo io)
    {
        var schema = new Option<string>("--schema") { Description = "The schema snapshot now.", Required = true };
        var recordings = new Option<string>("--recordings") { Description = "The folder of recordings (<head>/<test>.json).", Required = true };
        var versions = new Option<string[]>("--version") { Description = "A version that shapes the SQL, name=value (ef=10.0.12); compared with the recordings' (repeatable).", AllowMultipleArgumentsPerToken = true };
        var command = new Command("stale", "The recordings a schema change made stale, and what changed, from their stamps without replaying: the tests to re-record. Exit 1 when any is stale.") { schema, recordings, versions };
        command.Action(io, (parsed, cancellationToken) =>
        {
            var snapshot = SchemaSnapshot.Load(Path.GetFullPath(parsed.GetValue(schema)!, io.CurrentDirectory));
            var given = (parsed.GetValue(versions) ?? []).Select(item => item.Split(['='], 2)).Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
            var folder = Path.GetFullPath(parsed.GetValue(recordings)!, io.CurrentDirectory);
            var states = RecordingCheck.Check(folder, snapshot.ToSchema(given));
            foreach (var state in states.Where(state => state.IsStale))
            {
                io.Out.WriteLine(Path.GetRelativePath(folder, state.Path));
                foreach (var difference in state.Differences)
                {
                    io.Out.WriteLine("  " + difference);
                }
            }

            var unlisted = states.SelectMany(state => state.Objects).Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => snapshot.Objects.TryGetValue(name, out var item) && item.Dynamic && !item.Listed)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            if (unlisted.Count > 0)
            {
                io.Error.WriteLine($"warning: recordings touch dynamic SQL with no references listed, so a change to what it reads is missed: {string.Join(", ", unlisted)}");
            }

            var stale = states.Count(state => state.IsStale);
            io.Error.WriteLine($"{stale} of {states.Count} recordings stale");
            return Task.FromResult(stale > 0 ? 1 : 0);
        });
        return command;
    }

    private static Command StartCommand(CliIo io)
    {
        var connection = Connection();
        var folder = new Option<string?>("--folder") { Description = "A folder on the server for the session's files. Default: the server's log folder." };
        var state = State();
        var like = new Option<string?>("--database-like") { Description = "Capture every database whose name is LIKE this (TestDb_%), for a suite that makes one per run; default: the connection's." };
        var command = new Command("start", "Start the session on the connection's database and send the marker that lines the clocks up. Needs ALTER ANY EVENT SESSION.") { connection, folder, state, like };
        command.Action(io, async (parsed, cancellationToken) =>
        {
            await using var sql = await OpenAsync(parsed.GetValue(connection)!, cancellationToken);
            var where = parsed.GetValue(folder) ?? await DefaultFolderAsync(sql, cancellationToken);
            var started = await ExtendedEvents.StartAsync(sql, where, cancellationToken, parsed.GetValue(like));
            var to = Path.GetFullPath(parsed.GetValue(state)!, io.CurrentDirectory);
            started.Save(to);
            io.Error.WriteLine($"Session {started.Session} on {started.Database}, its files {started.Files}; state in {to}");
            return 0;
        });
        return command;
    }

    private static Command StopCommand(CliIo io)
    {
        var connection = Connection();
        var state = State();
        var command = new Command("stop", "Stop and drop the session; its files stay for read.") { connection, state };
        command.Action(io, async (parsed, cancellationToken) =>
        {
            var started = CaptureState.Load(Path.GetFullPath(parsed.GetValue(state)!, io.CurrentDirectory));
            await using var sql = await OpenAsync(parsed.GetValue(connection)!, cancellationToken);
            await ExtendedEvents.StopAsync(sql, started.Database, cancellationToken);
            io.Error.WriteLine($"Session {started.Session} stopped.");
            return 0;
        });
        return command;
    }

    private static Command ReadCommand(CliIo io)
    {
        var connection = Connection();
        var state = State();
        var output = new Option<string>("--out") { Description = "The log folder to write to.", DefaultValueFactory = _ => "sql-log" };
        var command = new Command("read", "Read the session's files through the server into the log, on this machine's clock.") { connection, state, output };
        command.Action(io, async (parsed, cancellationToken) =>
        {
            var started = CaptureState.Load(Path.GetFullPath(parsed.GetValue(state)!, io.CurrentDirectory));
            await using var sql = await OpenAsync(parsed.GetValue(connection)!, cancellationToken);
            var entries = await ExtendedEvents.ReadAsync(sql, started, cancellationToken);
            var folder = Path.GetFullPath(parsed.GetValue(output)!, io.CurrentDirectory);
            var log = new SqlLog(folder);
            foreach (var entry in entries)
            {
                log.Write(entry);
            }

            io.Error.WriteLine($"{entries.Count} commands into {folder}");
            return 0;
        });
        return command;
    }

    private static Command ReportCommand(CliIo io)
    {
        var logs = new Option<string[]>("--log") { Description = "Log folders or files (*.jsonl).", Required = true, AllowMultipleArgumentsPerToken = true };
        var timings = new Option<string[]>("--timings") { Description = "The run's timings (BRUINELL_TIMINGS): their test spans join the commands to the tests.", AllowMultipleArgumentsPerToken = true };
        var schema = new Option<string?>("--schema") { Description = "The schema snapshot: a procedure's or view's tables count for the tests that use it." };
        var output = new Option<string>("--out") { Description = "The folder for report.md, report.json and sql-touch-map.json.", DefaultValueFactory = _ => "sql-report" };
        var command = new Command("report", "The database's share of each test, how far each object reaches, and the commands repeated within a test.") { logs, timings, schema, output };
        command.Action(io, parsed =>
        {
            var entries = parsed.GetValue(logs)!.SelectMany(path => SqlLog.Read(Path.GetFullPath(path, io.CurrentDirectory))).ToList();
            var csv = (parsed.GetValue(timings) ?? []).SelectMany(path => TouchMap.CsvFiles(Path.GetFullPath(path, io.CurrentDirectory))).ToList();
            var snapshot = parsed.GetValue(schema) is { } file ? SchemaSnapshot.Load(Path.GetFullPath(file, io.CurrentDirectory)) : null;
            var report = SqlReport.Of(entries, SqlReport.Windows(csv), snapshot);
            var folder = Path.GetFullPath(parsed.GetValue(output)!, io.CurrentDirectory);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "report.md"), report.ToMarkdown());
            File.WriteAllText(Path.Combine(folder, "report.json"), report.ToJson());
            File.WriteAllText(Path.Combine(folder, "sql-touch-map.json"), CommonOptions.ToJson(report.TouchMap()));
            io.Error.WriteLine($"{report.Tests.Count} tests, {entries.Count} commands, {report.Objects.Count} objects; wrote {folder}");
            return 0;
        });
        return command;
    }

    private static Option<string> Connection() => new("--connection") { Description = "A connection string to the test database.", Required = true };

    private static Option<string> State() => new("--state") { Description = "The capture's state file.", DefaultValueFactory = _ => StateFile };

    private static async Task<SqlConnection> OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<string> DefaultFolderAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS nvarchar(4000))", connection);
        return (string)(await command.ExecuteScalarAsync(cancellationToken))!;
    }
}
