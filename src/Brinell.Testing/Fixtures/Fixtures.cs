using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using Xunit;

namespace Brinell.Testing.Fixtures;

/// <summary>
/// Database fixture for EF Core testing.
/// Manages in-memory SQLite database lifecycle.
/// </summary>
/// <typeparam name="TDbContext">The DbContext type.</typeparam>
public class DatabaseFixture<TDbContext> : IAsyncLifetime where TDbContext : DbContext
{
    private readonly Func<DbContextOptions<TDbContext>, TDbContext> _contextFactory;
    private SqliteConnection? _connection;
    public TDbContext Context { get; private set; } = null!;

    /// <summary>
    /// Create fixture with context factory.
    /// </summary>
    public DatabaseFixture(Func<DbContextOptions<TDbContext>, TDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Initialize database.
    /// </summary>
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = _contextFactory(options);
        await Context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Cleanup database.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    /// <summary>
    /// Seed data.
    /// </summary>
    public async Task SeedAsync<TEntity>(params TEntity[] entities) where TEntity : class
    {
        Context.AddRange(entities);
        await Context.SaveChangesAsync();
    }

    /// <summary>
    /// Reset database (clear all data).
    /// </summary>
    public async Task ResetAsync()
    {
        foreach (var entity in Context.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                await Context.Database.ExecuteSqlAsync($"DELETE FROM {tableName}");
            }
        }
    }
}

/// <summary>
/// API server fixture for integration testing.
/// Manages test HTTP server lifecycle with dependency injection.
/// 
/// Note: Requires WebApplicationFactory{Program} from the application under test.
/// Configure the program type when inheriting or using this fixture.
/// </summary>
public class ApiServerFixture : IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = null!;

    /// <summary>
    /// Initialize HTTP client.
    /// </summary>
    public Task InitializeAsync()
    {
        // TODO: Implement with actual WebApplicationFactory{Program}
        // HttpClient = _factory.CreateClient();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup server.
    /// </summary>
    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Make GET request.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content);
    }

    /// <summary>
    /// Make POST request.
    /// </summary>
    public async Task<T?> PostAsync<T>(string url, object data)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            "application/json");

        var response = await HttpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent);
    }
}

/// <summary>
/// SignalR fixture for real-time communication testing.
/// Manages hub connection lifecycle.
/// 
/// Note: Requires HubConnection and WebApplicationFactory{Program} from SignalR.
/// </summary>
public class SignalRFixture : IAsyncLifetime
{
    public string ConnectionId { get; private set; } = string.Empty;

    /// <summary>
    /// Initialize SignalR connection.
    /// </summary>
    public async Task InitializeAsync()
    {
        // TODO: Implement with actual HubConnection
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup connection.
    /// </summary>
    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Send message to hub.
    /// </summary>
    public async Task SendAsync(string method, params object[] args)
    {
        // TODO: Implement actual SignalR sending
        await Task.CompletedTask;
    }
}

/// <summary>
/// Application fixture for full integration testing.
/// Manages complete application lifecycle.
/// 
/// Note: Requires WebApplicationFactory{Program} from the application under test.
/// </summary>
public class ApplicationFixture : IAsyncLifetime
{
    public HttpClient HttpClient { get; private set; } = null!;
    public object? SignalRConnection { get; private set; }

    /// <summary>
    /// Initialize application.
    /// </summary>
    public async Task InitializeAsync()
    {
        // TODO: Implement with actual WebApplicationFactory{Program}
        // HttpClient = _factory.CreateClient();
        // ... initialize SignalR connection
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleanup application.
    /// </summary>
    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Reset application state.
    /// </summary>
    public async Task ResetAsync()
    {
        await Task.CompletedTask;
    }

    /// <summary>
    /// Marker interface for test-injected services.
    /// </summary>
    public interface ITestMarker { }

    private class TestMarker : ITestMarker { }
}
