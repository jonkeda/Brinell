using System.Data;
using System.Data.Common;

namespace Bruinet.Mocking.Data.Replay;

/// <summary>
/// A connection that records its commands into a <see cref="ReplaySession"/>, or answers them from one with no database:
/// in replay the real connection is never opened, and only makes the commands and parameters the app's code expects.
/// For code that reaches the database through <see cref="DbConnection"/> (ADO.NET, Dapper); EF has its interceptor.
/// </summary>
public sealed class ReplayingDbConnection(DbConnection inner, ReplaySession session) : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    public DbConnection Inner { get; } = inner;

    public ReplaySession Session { get; } = session;

    private bool Replaying => Session.Mode == ReplayMode.Replay;

#pragma warning disable CS8765 // ConnectionString's setter allows null in some targets and not others.
    public override string ConnectionString
    {
        get => Inner.ConnectionString;
        set => Inner.ConnectionString = value;
    }
#pragma warning restore CS8765

    public override string Database => Inner.Database;

    public override string DataSource => Inner.DataSource;

    public override string ServerVersion => Replaying ? string.Empty : Inner.ServerVersion;

    public override ConnectionState State => Replaying ? _state : Inner.State;

    public override int ConnectionTimeout => Inner.ConnectionTimeout;

    public override void ChangeDatabase(string databaseName)
    {
        if (!Replaying)
        {
            Inner.ChangeDatabase(databaseName);
        }
    }

    public override void Open()
    {
        if (Replaying)
        {
            _state = ConnectionState.Open;
        }
        else
        {
            Inner.Open();
        }
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        if (Replaying)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }

        return Inner.OpenAsync(cancellationToken);
    }

    public override void Close()
    {
        _state = ConnectionState.Closed;
        if (!Replaying)
        {
            Inner.Close();
        }
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => new ReplayingDbTransaction(Replaying ? null : Inner.BeginTransaction(isolationLevel), this, isolationLevel);

    protected override DbCommand CreateDbCommand() => new ReplayingDbCommand(Inner.CreateCommand(), this);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>The real transaction in record mode; in replay one with nothing behind it, as the writes it holds are recorded.</summary>
public sealed class ReplayingDbTransaction(DbTransaction? inner, ReplayingDbConnection connection, IsolationLevel isolationLevel) : DbTransaction
{
    public DbTransaction? Inner { get; } = inner;

    protected override DbConnection DbConnection => connection;

    public override IsolationLevel IsolationLevel => isolationLevel;

    public override void Commit() => Inner?.Commit();

    public override void Rollback() => Inner?.Rollback();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner?.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// A command run against the database and recorded, or answered from the recording: rows from a <see cref="ReplayDataReader"/>
/// in both modes, so the app reads alike; outputs and return values given back to its parameters; a recorded error raised
/// as the provider's own exception.
/// </summary>
public sealed class ReplayingDbCommand : DbCommand
{
    private ReplayingDbConnection? _connection;
    private ReplayingDbTransaction? _transaction;

    internal ReplayingDbCommand(DbCommand inner, ReplayingDbConnection? connection)
    {
        Inner = inner;
        _connection = connection;
    }

    public DbCommand Inner { get; }

#pragma warning disable CS8765 // CommandText's setter allows null in some targets and not others.
    public override string CommandText
    {
        get => Inner.CommandText;
        set => Inner.CommandText = value;
    }
#pragma warning restore CS8765

    public override int CommandTimeout
    {
        get => Inner.CommandTimeout;
        set => Inner.CommandTimeout = value;
    }

    public override CommandType CommandType
    {
        get => Inner.CommandType;
        set => Inner.CommandType = value;
    }

    public override UpdateRowSource UpdatedRowSource
    {
        get => Inner.UpdatedRowSource;
        set => Inner.UpdatedRowSource = value;
    }

    public override bool DesignTimeVisible
    {
        get => Inner.DesignTimeVisible;
        set => Inner.DesignTimeVisible = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set
        {
            _connection = value as ReplayingDbConnection;
            Inner.Connection = value is ReplayingDbConnection replaying ? replaying.Inner : value;
        }
    }

    protected override DbParameterCollection DbParameterCollection => Inner.Parameters;

    protected override DbTransaction? DbTransaction
    {
        get => _transaction;
        set
        {
            _transaction = value as ReplayingDbTransaction;
            Inner.Transaction = value is ReplayingDbTransaction replaying ? replaying.Inner : value;
        }
    }

    private ReplaySession Session => _connection?.Session ?? throw new InvalidOperationException("The command has no replaying connection.");

    public override void Cancel()
    {
        if (Session.Mode == ReplayMode.Record)
        {
            Inner.Cancel();
        }
    }

    public override void Prepare()
    {
        if (Session.Mode == ReplayMode.Record)
        {
            Inner.Prepare();
        }
    }

    protected override DbParameter CreateDbParameter() => Inner.CreateParameter();

    public override int ExecuteNonQuery()
    {
        if (Session.Mode == ReplayMode.Replay)
        {
            return ProviderRules.Replay(Session, this, "non-query").RecordsAffected;
        }

        var exchange = ProviderRules.Live(this, "non-query");
        return Record(exchange, () => exchange.RecordsAffected = Inner.ExecuteNonQuery()).RecordsAffected;
    }

    public override object? ExecuteScalar()
    {
        if (Session.Mode == ReplayMode.Replay)
        {
            return Scalar(ProviderRules.Replay(Session, this, "scalar"));
        }

        var exchange = ProviderRules.Live(this, "scalar");
        return Scalar(Record(exchange, () => exchange.Scalar = Values.ToJson(Inner.ExecuteScalar())));
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        if (Session.Mode == ReplayMode.Replay)
        {
            return Reader(ProviderRules.Replay(Session, this, "reader"));
        }

        var exchange = ProviderRules.Live(this, "reader");
        return Reader(Record(exchange, () =>
        {
            using var reader = Inner.ExecuteReader(behavior);
            exchange.Results = ProviderRules.ReadAll(reader);
            reader.Close();
            exchange.RecordsAffected = reader.RecordsAffected;
        }));
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        if (Session.Mode == ReplayMode.Replay)
        {
            return ProviderRules.Replay(Session, this, "non-query").RecordsAffected;
        }

        var exchange = ProviderRules.Live(this, "non-query");
        return (await RecordAsync(exchange, async () => exchange.RecordsAffected = await Inner.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false)).ConfigureAwait(false)).RecordsAffected;
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        if (Session.Mode == ReplayMode.Replay)
        {
            return Scalar(ProviderRules.Replay(Session, this, "scalar"));
        }

        var exchange = ProviderRules.Live(this, "scalar");
        return Scalar(await RecordAsync(exchange, async () => exchange.Scalar = Values.ToJson(await Inner.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))).ConfigureAwait(false));
    }

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        if (Session.Mode == ReplayMode.Replay)
        {
            return Reader(ProviderRules.Replay(Session, this, "reader"));
        }

        var exchange = ProviderRules.Live(this, "reader");
        return Reader(await RecordAsync(exchange, async () =>
        {
            using var reader = await Inner.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            exchange.Results = ProviderRules.ReadAll(reader);
            reader.Close();
            exchange.RecordsAffected = reader.RecordsAffected;
        }).ConfigureAwait(false));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
        }

        base.Dispose(disposing);
    }

    private static ReplayDataReader Reader(Exchange exchange) => new(exchange.Results ?? [], exchange.RecordsAffected);

    private object Scalar(Exchange exchange) => Values.FromJson(Session.Bound(exchange.Scalar)) ?? DBNull.Value;

    // Outputs are read once the command is done (a reader's once it is closed); an error is recorded and thrown on.
    private Exchange Record(Exchange exchange, Action run)
    {
        try
        {
            run();
            exchange.Outputs = ProviderRules.Outputs(this);
            return exchange;
        }
        catch (DbException error)
        {
            exchange.Error = ProviderRules.Recorded(error);
            throw;
        }
        finally
        {
            Session.Record(exchange);
        }
    }

    private async Task<Exchange> RecordAsync(Exchange exchange, Func<Task> run)
    {
        try
        {
            await run().ConfigureAwait(false);
            exchange.Outputs = ProviderRules.Outputs(this);
            return exchange;
        }
        catch (DbException error)
        {
            exchange.Error = ProviderRules.Recorded(error);
            throw;
        }
        finally
        {
            Session.Record(exchange);
        }
    }
}

/// <summary>
/// The provider's factory for code that makes its connections itself: a connection made inside a test's
/// <see cref="ReplayScope"/> records or replays in that test's session, one made outside is the real one. One factory
/// serves every test running in parallel.
/// </summary>
public sealed class ReplayingDbProviderFactory(DbProviderFactory inner) : DbProviderFactory
{
    public override DbConnection? CreateConnection()
    {
        var connection = inner.CreateConnection();
        return connection is not null && ReplayScope.Current is { } session ? new ReplayingDbConnection(connection, session) : connection;
    }

    public override DbCommand? CreateCommand()
    {
        var command = inner.CreateCommand();
        return command is not null && ReplayScope.Current is not null ? new ReplayingDbCommand(command, null) : command;
    }

    public override DbParameter? CreateParameter() => inner.CreateParameter();

    public override DbConnectionStringBuilder? CreateConnectionStringBuilder() => inner.CreateConnectionStringBuilder();

    public override DbCommandBuilder? CreateCommandBuilder() => inner.CreateCommandBuilder();

    public override DbDataAdapter? CreateDataAdapter() => inner.CreateDataAdapter();
}
