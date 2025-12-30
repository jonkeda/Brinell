using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Brinell.Stride.Communication;

/// <summary>
/// Named pipe implementation of IAutomationChannel for cross-process communication.
/// </summary>
public class NamedPipeChannel : IAutomationChannel
{
    private readonly string _pipeName;
    private readonly JsonSerializerOptions _jsonOptions;
    private NamedPipeClientStream? _pipeClient;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    /// <summary>
    /// Default pipe name for Brinell automation.
    /// </summary>
    public const string DefaultPipeName = "Brinell.Stride.Automation";

    /// <inheritdoc />
    public bool IsConnected => _pipeClient?.IsConnected ?? false;

    /// <summary>
    /// Create a new named pipe channel.
    /// </summary>
    /// <param name="pipeName">Name of the pipe to connect to.</param>
    public NamedPipeChannel(string? pipeName = null)
    {
        _pipeName = pipeName ?? DefaultPipeName;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            return;
        }

        _pipeClient = new NamedPipeClientStream(
            ".",
            _pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await _pipeClient.ConnectAsync(linkedCts.Token);
            _reader = new StreamReader(_pipeClient, Encoding.UTF8, leaveOpen: true);
            _writer = new StreamWriter(_pipeClient, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"Failed to connect to pipe '{_pipeName}' within {timeout.TotalMilliseconds}ms");
        }
    }

    /// <inheritdoc />
    public async Task<AutomationResponse> SendCommandAsync(AutomationCommand command, CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _writer == null || _reader == null)
        {
            throw new InvalidOperationException("Not connected to game. Call ConnectAsync first.");
        }

        // Serialize and send command
        var json = JsonSerializer.Serialize(command, _jsonOptions);
        await _writer.WriteLineAsync(json.AsMemory(), cancellationToken);

        // Read response
        var responseJson = await _reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrEmpty(responseJson))
        {
            return AutomationResponse.Fail("Empty response from game");
        }

        try
        {
            return JsonSerializer.Deserialize<AutomationResponse>(responseJson, _jsonOptions)
                ?? AutomationResponse.Fail("Failed to deserialize response");
        }
        catch (JsonException ex)
        {
            return AutomationResponse.Fail($"Invalid JSON response: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync()
    {
        Dispose();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _pipeClient?.Dispose();

        _reader = null;
        _writer = null;
        _pipeClient = null;

        GC.SuppressFinalize(this);
    }
}
