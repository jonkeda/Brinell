using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Bruinell.Mocking.Data;

/// <summary>
/// A connection that logs every command and open to a <see cref="SqlLog"/>, and passes everything else to the real one.
/// For code that reaches the database through <see cref="DbConnection"/> (ADO.NET, Dapper); code typed to a provider's
/// own <c>SqlConnection</c> can't take it, and is logged from the server instead (Extended Events).
/// </summary>
public sealed class LoggingDbConnection(DbConnection inner, SqlLog log) : DbConnection
{
    /// <summary>The real connection.</summary>
    public DbConnection Inner { get; } = inner;

    internal SqlLog Log { get; } = log;

#pragma warning disable CS8765 // DbConnection.ConnectionString's setter allows null in some targets and not others.
    public override string ConnectionString
    {
        get => Inner.ConnectionString;
        set => Inner.ConnectionString = value;
    }
#pragma warning restore CS8765

    public override string Database => Inner.Database;

    public override string DataSource => Inner.DataSource;

    public override string ServerVersion => Inner.ServerVersion;

    public override ConnectionState State => Inner.State;

    public override int ConnectionTimeout => Inner.ConnectionTimeout;

    public override void ChangeDatabase(string databaseName) => Inner.ChangeDatabase(databaseName);

    public override void Close() => Inner.Close();

    public override void Open()
    {
        var start = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();
        Inner.Open();
        Log.Write(SqlLog.Entry("open", null, null, start, clock.Elapsed));
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();
        await Inner.OpenAsync(cancellationToken).ConfigureAwait(false);
        Log.Write(SqlLog.Entry("open", null, null, start, clock.Elapsed));
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => new LoggingDbTransaction(Inner.BeginTransaction(isolationLevel), this);

    protected override DbCommand CreateDbCommand() => new LoggingDbCommand(Inner.CreateCommand(), this);

    public override DataTable GetSchema() => Inner.GetSchema();

    public override DataTable GetSchema(string collectionName) => Inner.GetSchema(collectionName);

    public override DataTable GetSchema(string collectionName, string?[] restrictionValues) => Inner.GetSchema(collectionName, restrictionValues);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET
    protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
        => new LoggingDbTransaction(await Inner.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false), this);

    public override Task CloseAsync() => Inner.CloseAsync();

    public override async ValueTask DisposeAsync()
    {
        await Inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}

/// <summary>The real transaction, seen by the app as its <see cref="LoggingDbConnection"/>'s.</summary>
public sealed class LoggingDbTransaction(DbTransaction inner, LoggingDbConnection connection) : DbTransaction
{
    public DbTransaction Inner { get; } = inner;

    protected override DbConnection DbConnection => connection;

    public override IsolationLevel IsolationLevel => Inner.IsolationLevel;

    public override void Commit() => Inner.Commit();

    public override void Rollback() => Inner.Rollback();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
        }

        base.Dispose(disposing);
    }

#if NET
    public override Task CommitAsync(CancellationToken cancellationToken = default) => Inner.CommitAsync(cancellationToken);

    public override Task RollbackAsync(CancellationToken cancellationToken = default) => Inner.RollbackAsync(cancellationToken);

    public override async ValueTask DisposeAsync()
    {
        await Inner.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif
}

/// <summary>A command that times itself and logs on its <see cref="LoggingDbConnection"/>'s log; a reader logs when closed.</summary>
public sealed class LoggingDbCommand : DbCommand
{
    private LoggingDbConnection? _connection;
    private LoggingDbTransaction? _transaction;

    internal LoggingDbCommand(DbCommand inner, LoggingDbConnection? connection)
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
            _connection = value as LoggingDbConnection;
            Inner.Connection = value is LoggingDbConnection logging ? logging.Inner : value;
        }
    }

    protected override DbParameterCollection DbParameterCollection => Inner.Parameters;

    // The transaction the app set, as it set it; the real one on the real command.
    protected override DbTransaction? DbTransaction
    {
        get => _transaction is { } transaction && ReferenceEquals(transaction.Inner, Inner.Transaction) ? transaction : Inner.Transaction;
        set
        {
            _transaction = value as LoggingDbTransaction;
            Inner.Transaction = value is LoggingDbTransaction logging ? logging.Inner : value;
        }
    }

    public override void Cancel() => Inner.Cancel();

    public override void Prepare() => Inner.Prepare();

    protected override DbParameter CreateDbParameter() => Inner.CreateParameter();

    public override int ExecuteNonQuery() => Run("non-query", Inner.ExecuteNonQuery, (entry, affected) => entry.Affected = affected);

    public override object? ExecuteScalar() => Run<object?>("scalar", Inner.ExecuteScalar, null);

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var start = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();
        try
        {
            return new LoggingDbDataReader(Inner.ExecuteReader(behavior), this, start, clock);
        }
        catch (DbException error)
        {
            Failed(error, start, clock.Elapsed);
            throw;
        }
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        => await RunAsync("non-query", () => Inner.ExecuteNonQueryAsync(cancellationToken), (entry, affected) => entry.Affected = affected).ConfigureAwait(false);

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        => await RunAsync<object?>("scalar", () => Inner.ExecuteScalarAsync(cancellationToken)!, null).ConfigureAwait(false);

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();
        try
        {
            return new LoggingDbDataReader(await Inner.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false), this, start, clock);
        }
        catch (DbException error)
        {
            Failed(error, start, clock.Elapsed);
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The entry for this command as it is now, its text a stored procedure's <c>EXEC</c> when called as one.</summary>
    internal SqlLogEntry Entry(string kind, DateTime start, TimeSpan elapsed)
        => SqlLog.Entry(
            kind,
            CommandType == CommandType.StoredProcedure ? "EXEC " + CommandText : CommandText,
            Inner.Parameters.Cast<DbParameter>().Select(parameter => parameter.ParameterName),
            start,
            elapsed);

    internal void Write(SqlLogEntry entry) => _connection?.Log.Write(entry);

    private T Run<T>(string kind, Func<T> run, Action<SqlLogEntry, T>? describe)
    {
        var start = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();
        T result;
        try
        {
            result = run();
        }
        catch (DbException error)
        {
            Failed(error, start, clock.Elapsed);
            throw;
        }

        var entry = Entry(kind, start, clock.Elapsed);
        describe?.Invoke(entry, result);
        Write(entry);
        return result;
    }

    private async Task<T> RunAsync<T>(string kind, Func<Task<T>> run, Action<SqlLogEntry, T>? describe)
    {
        var start = DateTime.UtcNow;
        var clock = Stopwatch.StartNew();
        T result;
        try
        {
            result = await run().ConfigureAwait(false);
        }
        catch (DbException error)
        {
            Failed(error, start, clock.Elapsed);
            throw;
        }

        var entry = Entry(kind, start, clock.Elapsed);
        describe?.Invoke(entry, result);
        Write(entry);
        return result;
    }

    private void Failed(DbException error, DateTime start, TimeSpan elapsed)
    {
        var entry = Entry("error", start, elapsed);
        entry.Error = ErrorNumber(error);
        Write(entry);
    }

    /// <summary>
    /// The provider's number for <paramref name="error"/>: SqlException's <c>Number</c>, SqliteException's
    /// <c>SqliteErrorCode</c>, else the HRESULT; read by name, so no provider is referenced.
    /// </summary>
    public static int ErrorNumber(DbException error)
        => error.GetType().GetProperty("Number")?.GetValue(error) as int?
            ?? error.GetType().GetProperty("SqliteErrorCode")?.GetValue(error) as int?
            ?? error.ErrorCode;
}

/// <summary>The real reader, counting its rows; logs its command when closed, so reading the rows counts in its time.</summary>
public sealed class LoggingDbDataReader : DbDataReader
{
    private readonly LoggingDbCommand _command;
    private readonly DateTime _start;
    private readonly Stopwatch _clock;
    private int _rows;
    private int _logged;

    internal LoggingDbDataReader(DbDataReader inner, LoggingDbCommand command, DateTime start, Stopwatch clock)
    {
        Inner = inner;
        _command = command;
        _start = start;
        _clock = clock;
    }

    public DbDataReader Inner { get; }

    public override int FieldCount => Inner.FieldCount;

    public override int VisibleFieldCount => Inner.VisibleFieldCount;

    public override bool HasRows => Inner.HasRows;

    public override bool IsClosed => Inner.IsClosed;

    public override int RecordsAffected => Inner.RecordsAffected;

    public override int Depth => Inner.Depth;

    public override object this[int ordinal] => Inner[ordinal];

    public override object this[string name] => Inner[name];

    public override bool Read()
    {
        var read = Inner.Read();
        if (read)
        {
            _rows++;
        }

        return read;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        var read = await Inner.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (read)
        {
            _rows++;
        }

        return read;
    }

    public override bool NextResult() => Inner.NextResult();

    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Inner.NextResultAsync(cancellationToken);

    public override void Close()
    {
        Inner.Close();
        Logged();
    }

    public override string GetName(int ordinal) => Inner.GetName(ordinal);

    public override int GetOrdinal(string name) => Inner.GetOrdinal(name);

    public override string GetDataTypeName(int ordinal) => Inner.GetDataTypeName(ordinal);

    public override Type GetFieldType(int ordinal) => Inner.GetFieldType(ordinal);

    public override object GetValue(int ordinal) => Inner.GetValue(ordinal);

    public override int GetValues(object[] values) => Inner.GetValues(values);

    public override bool IsDBNull(int ordinal) => Inner.IsDBNull(ordinal);

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => Inner.IsDBNullAsync(ordinal, cancellationToken);

    public override T GetFieldValue<T>(int ordinal) => Inner.GetFieldValue<T>(ordinal);

    public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => Inner.GetFieldValueAsync<T>(ordinal, cancellationToken);

    public override bool GetBoolean(int ordinal) => Inner.GetBoolean(ordinal);

    public override byte GetByte(int ordinal) => Inner.GetByte(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => Inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    public override char GetChar(int ordinal) => Inner.GetChar(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => Inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    public override DateTime GetDateTime(int ordinal) => Inner.GetDateTime(ordinal);

    public override decimal GetDecimal(int ordinal) => Inner.GetDecimal(ordinal);

    public override double GetDouble(int ordinal) => Inner.GetDouble(ordinal);

    public override float GetFloat(int ordinal) => Inner.GetFloat(ordinal);

    public override Guid GetGuid(int ordinal) => Inner.GetGuid(ordinal);

    public override short GetInt16(int ordinal) => Inner.GetInt16(ordinal);

    public override int GetInt32(int ordinal) => Inner.GetInt32(ordinal);

    public override long GetInt64(int ordinal) => Inner.GetInt64(ordinal);

    public override string GetString(int ordinal) => Inner.GetString(ordinal);

    public override Stream GetStream(int ordinal) => Inner.GetStream(ordinal);

    public override TextReader GetTextReader(int ordinal) => Inner.GetTextReader(ordinal);

    public override DataTable? GetSchemaTable() => Inner.GetSchemaTable();

    public override System.Collections.IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Inner.Dispose();
            Logged();
        }

        base.Dispose(disposing);
    }

#if NET
    public override async ValueTask DisposeAsync()
    {
        await Inner.DisposeAsync().ConfigureAwait(false);
        Logged();
        await base.DisposeAsync().ConfigureAwait(false);
    }
#endif

    private void Logged()
    {
        if (Interlocked.Exchange(ref _logged, 1) == 0)
        {
            var entry = _command.Entry("reader", _start, _clock.Elapsed);
            entry.Rows = _rows;
            var affected = Inner.RecordsAffected;
            entry.Affected = affected >= 0 ? affected : null;
            _command.Write(entry);
        }
    }
}

/// <summary>
/// A provider factory whose connections log: for an app that makes its connections through a
/// <see cref="DbProviderFactory"/>, such as a .NET Framework app's <c>DbProviderFactories</c> entry.
/// </summary>
public sealed class LoggingDbProviderFactory(DbProviderFactory inner, SqlLog log) : DbProviderFactory
{
    public override DbConnection? CreateConnection() => inner.CreateConnection() is { } connection ? new LoggingDbConnection(connection, log) : null;

    public override DbCommand? CreateCommand() => inner.CreateCommand() is { } command ? new LoggingDbCommand(command, null) : null;

    public override DbParameter? CreateParameter() => inner.CreateParameter();

    public override DbConnectionStringBuilder? CreateConnectionStringBuilder() => inner.CreateConnectionStringBuilder();

    public override DbCommandBuilder? CreateCommandBuilder() => inner.CreateCommandBuilder();

    public override DbDataAdapter? CreateDataAdapter() => inner.CreateDataAdapter();
}
