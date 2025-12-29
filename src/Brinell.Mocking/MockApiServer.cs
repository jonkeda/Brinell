using WireMock.Server;
using WireMock.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Brinell.Mocking;

/// <summary>
/// Mock API server wrapper using WireMock.
/// </summary>
public class MockApiServer : IDisposable
{
    private WireMockServer? _server;
    private bool _disposed;

    /// <summary>
    /// The base URL of the mock server.
    /// </summary>
    public string BaseUrl => _server?.Url ?? throw new InvalidOperationException("Server not started");
    
    /// <summary>
    /// The port the mock server is running on.
    /// </summary>
    public int Port => _server?.Port ?? throw new InvalidOperationException("Server not started");
    
    /// <summary>
    /// Check if server is running.
    /// </summary>
    public bool IsRunning => _server?.IsStarted ?? false;

    /// <summary>
    /// Start the mock server on a random available port.
    /// </summary>
    public void Start()
    {
        Start(0);
    }

    /// <summary>
    /// Start the mock server on a specific port (0 for random).
    /// </summary>
    public void Start(int port)
    {
        if (_server != null)
        {
            throw new InvalidOperationException("Server already started");
        }

        _server = WireMockServer.Start(new WireMockServerSettings
        {
            Port = port == 0 ? null : port,
            UseSSL = false
        });
    }

    /// <summary>
    /// Stop the mock server.
    /// </summary>
    public void Stop()
    {
        _server?.Stop();
        _server?.Dispose();
        _server = null;
    }

    /// <summary>
    /// Reset all stubs.
    /// </summary>
    public void Reset()
    {
        _server?.Reset();
    }

    /// <summary>
    /// Register a stub using the fluent builder.
    /// </summary>
    public MockApiServer Stub(Action<ApiStubBuilder> configure)
    {
        if (_server == null)
            throw new InvalidOperationException("Server not started");

        var builder = new ApiStubBuilder();
        configure(builder);
        
        var (request, response) = builder.Build();
        _server.Given(request).RespondWith(response);
        
        return this;
    }

    /// <summary>
    /// Register a simple GET stub.
    /// </summary>
    public MockApiServer StubGet(string path, object responseBody, int statusCode = 200)
    {
        return Stub(s => s
            .WithPath(path)
            .WithMethod("GET")
            .ReturnsJson(responseBody, statusCode));
    }

    /// <summary>
    /// Register a simple POST stub.
    /// </summary>
    public MockApiServer StubPost(string path, object responseBody, int statusCode = 200)
    {
        return Stub(s => s
            .WithPath(path)
            .WithMethod("POST")
            .ReturnsJson(responseBody, statusCode));
    }

    /// <summary>
    /// Verify a call was made to the given path.
    /// </summary>
    public bool VerifyCallMade(string path, string? method = null, int? expectedCount = null)
    {
        if (_server == null) return false;

        var entries = _server.LogEntries
            .Where(e => e.RequestMessage.Path == path);

        if (method != null)
        {
            entries = entries.Where(e => 
                e.RequestMessage.Method.Equals(method, StringComparison.OrdinalIgnoreCase));
        }

        var count = entries.Count();
        
        if (expectedCount.HasValue)
        {
            return count == expectedCount.Value;
        }
        
        return count > 0;
    }

    /// <summary>
    /// Get all request logs.
    /// </summary>
    public IEnumerable<RequestLog> GetRequestLogs()
    {
        if (_server == null) return Enumerable.Empty<RequestLog>();

        return _server.LogEntries.Select(e => new RequestLog
        {
            Path = e.RequestMessage.Path,
            Method = e.RequestMessage.Method,
            Body = e.RequestMessage.Body,
            Timestamp = e.RequestMessage.DateTime
        });
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Request log entry.
/// </summary>
public class RequestLog
{
    public string Path { get; init; } = "";
    public string Method { get; init; } = "";
    public string? Body { get; init; }
    public DateTime Timestamp { get; init; }
}
