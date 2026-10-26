using System.Data;
using System.Data.Common;
using Bruinet.Mocking.Data.Replay;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bruinet.Mocking.Data.EntityFrameworkCore;

/// <summary>
/// SQL replay for an EF Core context. Record: each command runs, its results are read whole into the session and EF gets
/// them back through a <see cref="ReplayDataReader"/>, as it will in replay. Replay: nothing reaches the database, the
/// connection isn't opened; each command is answered by the session, an error as the provider's own exception. The
/// session is resolved per command, so one interceptor serves tests in parallel; with none, the context runs as it would.
/// </summary>
public sealed class ReplayInterceptor(Func<ReplaySession?> session) : DbCommandInterceptor
{
    /// <summary>The interceptor of one session.</summary>
    public ReplayInterceptor(ReplaySession session)
        : this(() => session)
    {
    }

    /// <summary>Suppresses opening the connection in replay; added beside the command interceptor.</summary>
    public IInterceptor Connections => new ConnectionSuppressor(session);

    /// <summary>Gives EF a transaction with nothing behind it in replay, as the connection isn't open.</summary>
    public IInterceptor Transactions => new TransactionSuppressor(session);

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        => Replaying() is { } now ? InterceptionResult<DbDataReader>.SuppressWithResult(Reader(ProviderRules.Replay(now, command, "reader"))) : result;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ReaderExecuting(command, eventData, result));

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        if (Recording() is not { } now)
        {
            return result;
        }

        // Read whole, so the recording holds every row; outputs once the reader is closed.
        var exchange = ProviderRules.Live(command, "reader");
        using (result)
        {
            exchange.Results = ProviderRules.ReadAll(result);
            result.Close();
            exchange.RecordsAffected = result.RecordsAffected;
        }

        exchange.Outputs = ProviderRules.Outputs(command);
        now.Record(exchange);
        return Reader(exchange);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ReaderExecuted(command, eventData, result));

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        => Replaying() is { } now ? InterceptionResult<int>.SuppressWithResult(ProviderRules.Replay(now, command, "non-query").RecordsAffected) : result;

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(NonQueryExecuting(command, eventData, result));

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        if (Recording() is { } now)
        {
            var exchange = ProviderRules.Live(command, "non-query");
            exchange.RecordsAffected = result;
            exchange.Outputs = ProviderRules.Outputs(command);
            now.Record(exchange);
        }

        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(NonQueryExecuted(command, eventData, result));

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        => Replaying() is { } now
            ? InterceptionResult<object>.SuppressWithResult(Values.FromJson(now.Bound(ProviderRules.Replay(now, command, "scalar").Scalar)) ?? DBNull.Value)
            : result;

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ScalarExecuting(command, eventData, result));

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        if (Recording() is { } now)
        {
            var exchange = ProviderRules.Live(command, "scalar");
            exchange.Scalar = Values.ToJson(result);
            exchange.Outputs = ProviderRules.Outputs(command);
            now.Record(exchange);
        }

        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(ScalarExecuted(command, eventData, result));

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        if (Recording() is { } now && ProviderRules.Recorded(eventData.Exception) is { } error)
        {
            var exchange = ProviderRules.Live(command, Kind(eventData.ExecuteMethod));
            exchange.Error = error;
            now.Record(exchange);
        }
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        CommandFailed(command, eventData);
        return Task.CompletedTask;
    }

    private ReplaySession? Replaying() => session() is { Mode: ReplayMode.Replay } now ? now : null;

    private ReplaySession? Recording() => session() is { Mode: ReplayMode.Record } now ? now : null;

    private static ReplayDataReader Reader(Exchange exchange) => new(exchange.Results ?? [], exchange.RecordsAffected);

    private static string Kind(DbCommandMethod method) => method switch
    {
        DbCommandMethod.ExecuteNonQuery => "non-query",
        DbCommandMethod.ExecuteScalar => "scalar",
        _ => "reader",
    };

    private sealed class ConnectionSuppressor(Func<ReplaySession?> session) : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
            => session() is { Mode: ReplayMode.Replay } ? InterceptionResult.Suppress() : result;

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(ConnectionOpening(connection, eventData, result));
    }

    private sealed class TransactionSuppressor(Func<ReplaySession?> session) : DbTransactionInterceptor
    {
        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
            => session() is { Mode: ReplayMode.Replay } ? InterceptionResult<DbTransaction>.SuppressWithResult(new EmptyTransaction(connection, eventData.IsolationLevel)) : result;

        public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(TransactionStarting(connection, eventData, result));
    }

    // The writes it holds are recorded and answered one by one: committing, rolling back and savepoints have nothing to do.
    private sealed class EmptyTransaction(DbConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        protected override DbConnection DbConnection => connection;

        public override IsolationLevel IsolationLevel => isolationLevel;

        public override bool SupportsSavepoints => true;

        public override void Commit()
        {
        }

        public override void Rollback()
        {
        }

        public override void Save(string savepointName)
        {
        }

        public override void Rollback(string savepointName)
        {
        }

        public override void Release(string savepointName)
        {
        }
    }
}

/// <summary>Adds SQL replay to a context's options.</summary>
public static class ReplayExtensions
{
    /// <summary>Records into or replays from <paramref name="session"/>: contexts made with these options, one test's.</summary>
    public static DbContextOptionsBuilder UseSqlReplay(this DbContextOptionsBuilder options, ReplaySession session)
        => options.UseSqlReplay(() => session);

    /// <summary>
    /// Records into or replays from the session <paramref name="session"/> gives at each command (the test's, from its
    /// container); null runs the command against the database. Without it, the session of the flow's <see cref="ReplayScope"/>.
    /// </summary>
    public static DbContextOptionsBuilder UseSqlReplay(this DbContextOptionsBuilder options, Func<ReplaySession?>? session = null)
    {
        var interceptor = new ReplayInterceptor(session ?? (() => ReplayScope.Current));
        return options.AddInterceptors(interceptor, interceptor.Connections, interceptor.Transactions);
    }

    /// <inheritdoc cref="UseSqlReplay(DbContextOptionsBuilder, ReplaySession)"/>
    public static DbContextOptionsBuilder<TContext> UseSqlReplay<TContext>(this DbContextOptionsBuilder<TContext> options, ReplaySession session)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)options).UseSqlReplay(session);

    /// <inheritdoc cref="UseSqlReplay(DbContextOptionsBuilder, Func{ReplaySession?}?)"/>
    public static DbContextOptionsBuilder<TContext> UseSqlReplay<TContext>(this DbContextOptionsBuilder<TContext> options, Func<ReplaySession?>? session = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)((DbContextOptionsBuilder)options).UseSqlReplay(session);
}
