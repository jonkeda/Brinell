using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>One command the app sent: its text, parameters, how it ran, and how long it took.</summary>
public sealed record LoggedCommand(string Kind, string Text, IReadOnlyDictionary<string, string?> Parameters, double Ms);

/// <summary>
/// Every command and connection open the app makes through EF Core, with its time (.my/sql-replay/plan.md, 0.2): the
/// log replay is built on, and the database time the spike measures. A reader's time runs until it is disposed, so
/// reading the rows counts.
/// </summary>
public sealed class CommandLog : DbCommandInterceptor
{
    private readonly Lock _gate = new();
    private readonly List<LoggedCommand> _commands = [];
    private long _ticks;

    public CommandLog() => Connections = new ConnectionTimer(this);

    /// <summary>Times connection opens; added beside the command interceptor.</summary>
    public IInterceptor Connections { get; }

    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(0, Interlocked.Read(ref _ticks));

    public IReadOnlyList<LoggedCommand> Commands
    {
        get
        {
            lock (_gate)
            {
                return [.. _commands];
            }
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _commands.Clear();
        }

        Interlocked.Exchange(ref _ticks, 0);
    }

    /// <summary>The commands as JSON lines.</summary>
    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, Commands.Select(command => JsonSerializer.Serialize(command)));
    }

    public override InterceptionResult DataReaderDisposing(DbCommand command, DataReaderDisposingEventData eventData, InterceptionResult result)
    {
        Add("reader", command, eventData.Duration);
        return result;
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Add("non-query", command, eventData.Duration);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Add("non-query", command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Add("scalar", command, eventData.Duration);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Add("scalar", command, eventData.Duration);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        => Add("error", command, eventData.Duration);

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Add("error", command, eventData.Duration);
        return Task.CompletedTask;
    }

    private void Add(string kind, DbCommand command, TimeSpan duration)
    {
        var parameters = command.Parameters.Cast<DbParameter>()
            .ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value is null or DBNull ? null : Show(parameter.Value));
        lock (_gate)
        {
            _commands.Add(new LoggedCommand(kind, command.CommandText, parameters, duration.TotalMilliseconds));
        }

        AddTime(duration);
    }

    private void AddTime(TimeSpan duration) => Interlocked.Add(ref _ticks, (long)(duration.TotalSeconds * Stopwatch.Frequency));

    private static string Show(object value) => value is byte[] bytes ? Convert.ToBase64String(bytes) : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;

    private sealed class ConnectionTimer(CommandLog log) : DbConnectionInterceptor
    {
        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) => log.AddTime(eventData.Duration);

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            log.AddTime(eventData.Duration);
            return Task.CompletedTask;
        }
    }
}
