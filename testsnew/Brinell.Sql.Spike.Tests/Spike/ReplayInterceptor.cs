using System.Data.Common;
using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Brinell.Sql.Spike.Tests.Spike;

/// <summary>
/// The spike's EF adapter (.my/sql-replay/plan.md, 0.2). Record: each command runs, its results are read whole into
/// the recording and the app gets them back through a <see cref="ReplayDataReader"/>, as it will in replay. Replay:
/// nothing reaches the database; each command is answered by the session, an error as the provider's own exception.
/// </summary>
public sealed class ReplayInterceptor(ReplaySession session) : DbCommandInterceptor
{
    /// <summary>Suppresses opening the connection in replay; added beside the command interceptor.</summary>
    public IInterceptor Connections { get; } = new ConnectionSuppressor(session);

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        => session.Mode == ReplayMode.Replay ? InterceptionResult<DbDataReader>.SuppressWithResult(Reader(Replay(command, "reader"))) : result;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ReaderExecuting(command, eventData, result));

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        => session.Mode == ReplayMode.Record ? Recorded(command, result) : result;

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ReaderExecuted(command, eventData, result));

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        => session.Mode == ReplayMode.Replay ? InterceptionResult<int>.SuppressWithResult(Replay(command, "non-query").RecordsAffected) : result;

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(NonQueryExecuting(command, eventData, result));

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        if (session.Mode == ReplayMode.Record)
        {
            var exchange = Live(command, "non-query");
            exchange.RecordsAffected = result;
            session.Record(exchange);
        }

        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(NonQueryExecuted(command, eventData, result));

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        => session.Mode == ReplayMode.Replay ? InterceptionResult<object>.SuppressWithResult(Values.FromJson(Replay(command, "scalar").Scalar) ?? DBNull.Value) : result;

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ScalarExecuting(command, eventData, result));

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        if (session.Mode == ReplayMode.Record)
        {
            var exchange = Live(command, "scalar");
            exchange.Scalar = Values.ToJson(result);
            session.Record(exchange);
        }

        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ScalarExecuted(command, eventData, result));

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        if (session.Mode == ReplayMode.Record && eventData.Exception is SqlException error)
        {
            var exchange = Live(command, Kind(eventData.ExecuteMethod));
            exchange.Error = new RecordedError(nameof(SqlException), error.Number, error.State, error.Class, error.Message, error.Server);
            session.Record(exchange);
        }
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        CommandFailed(command, eventData);
        return Task.CompletedTask;
    }

    private Exchange Replay(DbCommand command, string kind)
    {
        var exchange = session.Answer(Live(command, kind));
        return exchange.Error is { } error ? throw SqlErrors.Create(error) : exchange;
    }

    private static ReplayDataReader Reader(Exchange exchange) => new(exchange.Results ?? [], exchange.RecordsAffected);

    // Read whole, so the recording holds every row; the app reads the same rows from a ReplayDataReader.
    private DbDataReader Recorded(DbCommand command, DbDataReader reader)
    {
        var results = new List<RecordedResult>();
        using (reader)
        {
            do
            {
                var columns = Enumerable.Range(0, reader.FieldCount).Select(i => new RecordedColumn(reader.GetName(i), FieldType(reader, i), reader.GetDataTypeName(i))).ToList();
                var rows = new List<IReadOnlyList<System.Text.Json.Nodes.JsonNode?>>();
                while (reader.Read())
                {
                    rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => Values.ToJson(Value(reader, i, columns[i])))]);
                }

                results.Add(new RecordedResult(columns, rows));
            }
            while (reader.NextResult());
        }

        var exchange = Live(command, "reader");
        exchange.Results = results;
        exchange.RecordsAffected = reader.RecordsAffected;
        session.Record(exchange);
        return Reader(exchange);
    }

    // A UDT (geography, geometry, hierarchyid) has no CLR type here: its bytes, as SqlClient gives them.
    private static bool IsUdt(RecordedColumn column) => column.ClrType == typeof(SqlBytes).FullName;

    private static string FieldType(DbDataReader reader, int ordinal)
    {
        try
        {
            return reader.GetFieldType(ordinal).FullName!;
        }
        catch (Exception)
        {
            return typeof(SqlBytes).FullName!;
        }
    }

    private static object? Value(DbDataReader reader, int ordinal, RecordedColumn column)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return IsUdt(column) || column.DataTypeName.EndsWith("geography", StringComparison.OrdinalIgnoreCase) || column.DataTypeName.EndsWith("geometry", StringComparison.OrdinalIgnoreCase)
            ? ((SqlDataReader)reader).GetSqlBytes(ordinal)
            : reader.GetValue(ordinal);
    }

    // A stored procedure called as one (CommandType.StoredProcedure, as ADO.NET code does) is kept as its EXEC, so it is
    // keyed and stamped as a procedure written out in text is.
    private static Exchange Live(DbCommand command, string kind) => new()
    {
        Kind = kind,
        Text = command.CommandType == System.Data.CommandType.StoredProcedure ? "EXEC " + command.CommandText : command.CommandText,
        Parameters = command.Parameters.Cast<DbParameter>().ToDictionary(parameter => parameter.ParameterName, parameter => Values.ToJson(parameter.Value)),
    };

    private static string Kind(DbCommandMethod method) => method switch
    {
        DbCommandMethod.ExecuteNonQuery => "non-query",
        DbCommandMethod.ExecuteScalar => "scalar",
        _ => "reader",
    };

    private sealed class ConnectionSuppressor(ReplaySession session) : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            => session.Mode == ReplayMode.Replay ? InterceptionResult.Suppress() : result;

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConnectionOpening(connection, eventData, result));
    }
}
