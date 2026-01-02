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
    /// Enable verbose logging. Default: false for production.
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
        int consecutiveErrors = 0;
        const int maxConsecutiveErrors = 5;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Use Byte mode for cross-platform compatibility and simpler line-based protocol
                using var pipeServer = new NamedPipeServerStream(
                    _options.PipeName,
                    PipeDirection.InOut,
                    _options.MaxConnections,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                Log("Waiting for connection...");
                consecutiveErrors = 0; // Reset on successful pipe creation
                
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
                    if (pipeServer.IsConnected)
                    {
                        pipeServer.Disconnect();
                    }
                    Log("Client disconnected");
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex) when (ex.Message.Contains("All pipe instances are busy"))
            {
                // This happens if server is started twice - stop this listener
                Log("Pipe instances busy - this listener will stop");
                break;
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                Log($"Error in listener ({consecutiveErrors}/{maxConsecutiveErrors}): {ex.Message}");
                
                if (consecutiveErrors >= maxConsecutiveErrors)
                {
                    Log("Too many consecutive errors, stopping listener");
                    break;
                }
                
                // Exponential backoff
                var delay = Math.Min(1000 * consecutiveErrors, 5000);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleConnectionAsync(NamedPipeServerStream pipe, CancellationToken cancellationToken)
    {
        Log("Setting up streams...");
        // Use simple constructors to avoid BOM issues
        var reader = new StreamReader(pipe);
        var writer = new StreamWriter(pipe) { AutoFlush = true };
        Log("Streams ready, entering message loop");

        try
        {
            while (pipe.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    Log("Waiting for command...");
                    line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    Log($"ReadLine returned: '{line ?? "(null)"}'");
                }
                catch (IOException ex)
                {
                    // Pipe closed by client
                    Log($"IOException during read: {ex.Message}");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Log("Read cancelled");
                    break;
                }
                
                if (string.IsNullOrEmpty(line))
                {
                    Log("Empty line received, closing connection");
                    break;
                }

                Log($"Received: {line}");

                AutomationResponse response;
                try
                {
                    var command = JsonSerializer.Deserialize<AutomationCommand>(line);
                    if (command != null)
                    {
                        Log($"Parsed command: Type={command.Type}, Method={command.Method}");
                        response = await _handler.HandleCommandAsync(command, cancellationToken).ConfigureAwait(false);
                        Log($"Handler returned: Success={response.Success}");
                    }
                    else
                    {
                        response = AutomationResponse.Fail("Failed to parse command");
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error processing command: {ex}");
                    response = AutomationResponse.Fail($"Error: {ex.Message}");
                }

                var responseJson = JsonSerializer.Serialize(response);
                Log($"Sending: {responseJson}");
                await writer.WriteLineAsync(responseJson).ConfigureAwait(false);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            // Don't dispose - the pipe owns the stream
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
