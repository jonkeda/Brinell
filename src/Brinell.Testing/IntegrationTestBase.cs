using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Testing;

/// <summary>
/// Base class for integration tests with database support.
/// Manages DbContext lifecycle with in-memory SQLite for test isolation.
/// </summary>
/// <typeparam name="TDbContext">The Entity Framework DbContext type.</typeparam>
public abstract class IntegrationTestBase<TDbContext> : TestBase<TDbContext> where TDbContext : DbContext
{
    /// <summary>
    /// Get the DbContext (alias for Context).
    /// </summary>
    public TDbContext DbContext => Context;

    protected IntegrationTestBase(ITestOutputHelper output) : base(output) { }

    /// <summary>
    /// Create DbContext configured with in-memory SQLite.
    /// Override to customize DbContext configuration.
    /// </summary>
    protected override TDbContext CreateContext()
    {
        var options = ConfigureDbContextOptions();
        var context = Activator.CreateInstance(typeof(TDbContext), options) as TDbContext
            ?? throw new InvalidOperationException($"Failed to create {typeof(TDbContext).Name}");
        return context;
    }

    /// <summary>
    /// Configure DbContext options (template method).
    /// Default uses in-memory SQLite.
    /// </summary>
    protected virtual DbContextOptions<TDbContext> ConfigureDbContextOptions()
    {
        var connectionString = $"DataSource=file::memory:?cache=shared&mode=rwc";
        var builder = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connectionString);

        return builder.Options;
    }

    /// <summary>
    /// Initialize database schema (override to customize).
    /// </summary>
    protected override async Task InitializeContextAsync()
    {
        // Create schema
        await DbContext.Database.EnsureCreatedAsync();
        LogAction("Database", "Schema created");

        // Seed data if implemented
        await SeedAsync();
    }

    /// <summary>
    /// Seed test data (template method).
    /// Override to provide default test data.
    /// </summary>
    protected virtual Task SeedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup database.
    /// </summary>
    protected override async Task CleanupContextAsync()
    {
        try
        {
            await DbContext.Database.EnsureDeletedAsync();
            LogAction("Database", "Cleaned up");
        }
        catch (Exception ex)
        {
            Log($"[WARNING] Failed to cleanup database: {ex.Message}");
        }

        await base.CleanupContextAsync();
    }

    #region Data Seeding

    /// <summary>
    /// Add a single entity and save changes.
    /// </summary>
    protected async Task AddAsync<TEntity>(TEntity entity) where TEntity : class
    {
        DbContext.Add(entity);
        await DbContext.SaveChangesAsync();
        LogAction("AddAsync", $"Added {typeof(TEntity).Name}");
    }

    /// <summary>
    /// Add multiple entities and save changes.
    /// </summary>
    protected async Task AddRangeAsync<TEntity>(params TEntity[] entities) where TEntity : class
    {
        DbContext.AddRange(entities);
        await DbContext.SaveChangesAsync();
        LogAction("AddRangeAsync", $"Added {entities.Length} {typeof(TEntity).Name}");
    }

    /// <summary>
    /// Seed data using async operation.
    /// </summary>
    protected async Task SeedDataAsync<TEntity>(params TEntity[] entities) where TEntity : class
    {
        await AddRangeAsync(entities);
        LogAssertion("SeedDataAsync", entities.Length.ToString(), "entities added", true);
    }

    #endregion

    #region Query Helpers

    /// <summary>
    /// Query all entities of a type.
    /// </summary>
    protected IQueryable<TEntity> Query<TEntity>() where TEntity : class
    {
        return DbContext.Set<TEntity>();
    }

    /// <summary>
    /// Get all entities (eager loaded).
    /// </summary>
    protected async Task<List<TEntity>> GetAllAsync<TEntity>() where TEntity : class
    {
        return await DbContext.Set<TEntity>().ToListAsync();
    }

    /// <summary>
    /// Get single entity by predicate.
    /// </summary>
    protected async Task<TEntity?> FindAsync<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
    {
        return (await GetAllAsync<TEntity>()).FirstOrDefault(predicate);
    }

    /// <summary>
    /// Count entities.
    /// </summary>
    protected async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        return await DbContext.Set<TEntity>().CountAsync();
    }

    #endregion

    #region Transaction Support

    /// <summary>
    /// Create a transaction scope for isolation.
    /// </summary>
    protected async Task<IDisposable> BeginTransactionAsync()
    {
        var transaction = await DbContext.Database.BeginTransactionAsync();
        LogAction("BeginTransaction", "Transaction started");
        return transaction;
    }

    /// <summary>
    /// Commit current transaction.
    /// </summary>
    protected async Task CommitAsync()
    {
        await DbContext.Database.CommitTransactionAsync();
        LogAction("CommitAsync", "Transaction committed");
    }

    /// <summary>
    /// Rollback current transaction.
    /// </summary>
    protected async Task RollbackAsync()
    {
        await DbContext.Database.RollbackTransactionAsync();
        LogAction("RollbackAsync", "Transaction rolled back");
    }

    #endregion

    #region Assertions

    /// <summary>
    /// Assert entity exists in database.
    /// </summary>
    protected async Task AssertExistsAsync<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
    {
        var found = await FindAsync(predicate);
        Assert.NotNull(found);
        LogAssertion("AssertExistsAsync", typeof(TEntity).Name, "found in database", true);
    }

    /// <summary>
    /// Assert entity does not exist in database.
    /// </summary>
    protected async Task AssertNotExistsAsync<TEntity>(Func<TEntity, bool> predicate) where TEntity : class
    {
        var found = await FindAsync(predicate);
        Assert.Null(found);
        LogAssertion("AssertNotExistsAsync", typeof(TEntity).Name, "not found in database", true);
    }

    /// <summary>
    /// Assert entity count matches expected.
    /// </summary>
    protected async Task AssertCountAsync<TEntity>(int expectedCount) where TEntity : class
    {
        var count = await CountAsync<TEntity>();
        Assert.Equal(expectedCount, count);
        LogAssertion("AssertCountAsync", count.ToString(), expectedCount.ToString(), true);
    }

    #endregion

    #region Refresh

    /// <summary>
    /// Refresh an entity from database.
    /// </summary>
    protected async Task RefreshAsync<TEntity>(TEntity entity) where TEntity : class
    {
        await DbContext.Entry(entity).ReloadAsync();
        LogAction("RefreshAsync", $"Reloaded {typeof(TEntity).Name}");
    }

    #endregion
}
