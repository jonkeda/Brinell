using Brinell.Stride.Communication;
using System.IO.Pipes;
using System.Text.Json;

namespace Brinell.Stride.Automation;

/// <summary>
/// Options for the automation server.
/// </summary>
public class AutomationServerOptions
{
    /// <summary>
    /// Named pipe name. Default: Brinell.Stride.Automation
    /// </summary>
    public string PipeName { get; set; } = "Brinell.Stride.Automation";

    /// <summary>
    /// Maximum concurrent connections.
    /// </summary>
    public int MaxConnections { get; set; } = 4;

    /// <summary>
    /// Enable verbose logging.
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
}

/// <summary>
/// Named pipe server that runs in the game process to receive automation commands.
/// </summary>
public sealed class AutomationServer : IDisposable
{
    private readonly AutomationServerOptions _options;
    private readonly IAutomationHandler _handler;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _listenerTasks = [];
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Whether the server is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Create a new automation server.
    /// </summary>
    public AutomationServer(IAutomationHandler handler, AutomationServerOptions? options = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _options = options ?? new AutomationServerOptions();
    }

    /// <summary>
    /// Start listening for automation connections.
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;

        for (int i = 0; i < _options.MaxConnections; i++)
        {
            var task = Task.Run(() => ListenAsync(_cts.Token));
            _listenerTasks.Add(task);
        }

        Log($"Automation server started on pipe '{_options.PipeName}' with {_options.MaxConnections} listeners");
    }

    /// <summary>
    /// Stop the automation server.
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cts.Cancel();

        try
        {
            await Task.WhenAll(_listenerTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }

        _listenerTasks.Clear();
        Log("Automation server stopped");
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility (PipeTransmissionMode.Message is Windows-only)
                using var pipeServer = new NamedPipeServerStream(
                    _options.PipeName,
                    PipeDirection.InOut,
                    _options.MaxConnections,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);
#pragma warning restore CA1416

                Log("Waiting for connection...");
                await pipeServer.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                Log("Client connected");

                try
                {
                    await HandleConnectionAsync(pipeServer, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log($"Error handling connection: {ex.Message}");
                }
                finally
                {
                    pipeServer.Disconnect();
                    Log("Client disconnected");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log($"Error in listener: {ex.Message}");
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(pipe);
        using var writer = new StreamWriter(pipe) { AutoFlush = true };

        while (pipe.IsConnected && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(line))
                break;

            Log($"Received: {line}");

            AutomationResponse response;
            try
            {
                var command = JsonSerializer.Deserialize<AutomationCommand>(line);
                if (command != null)
                {
                    response = await _handler.HandleCommandAsync(command, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    response = AutomationResponse.Fail("Failed to parse command");
                }
            }
            catch (Exception ex)
            {
                response = AutomationResponse.Fail($"Error: {ex.Message}");
            }

            var responseJson = JsonSerializer.Serialize(response);
            Log($"Sending: {responseJson}");
            await writer.WriteLineAsync(responseJson).ConfigureAwait(false);
        }
    }

    private void Log(string message)
    {
        if (_options.VerboseLogging)
        {
            Console.WriteLine($"[AutomationServer] {message}");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}
