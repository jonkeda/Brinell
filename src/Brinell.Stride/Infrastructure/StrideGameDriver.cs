using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Brinell.Stride.Infrastructure;

/// <summary>
/// Manages the lifecycle of the Stride game process for testing.
/// </summary>
public class StrideGameDriver : IDisposable
{
    private readonly StrideTestOptions _options;
    private Process? _gameProcess;
    private bool _disposed;

    /// <summary>
    /// The automation channel for communicating with the game.
    /// </summary>
    public Communication.IAutomationChannel? Channel { get; private set; }

    /// <summary>
    /// Whether the game is currently running.
    /// </summary>
    public bool IsRunning => _gameProcess?.HasExited == false;

    /// <summary>
    /// The game window handle (Windows only).
    /// </summary>
    public IntPtr GameWindowHandle => _gameProcess?.MainWindowHandle ?? IntPtr.Zero;

    /// <summary>
    /// Create a new game driver.
    /// </summary>
    public StrideGameDriver(StrideTestOptions? options = null)
    {
        _options = options ?? new StrideTestOptions();
    }

    /// <summary>
    /// Start the game and connect to automation.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.AttachToExisting)
        {
            await StartGameProcessAsync(cancellationToken);
        }

        await ConnectToAutomationAsync(cancellationToken);
    }

    /// <summary>
    /// Stop the game.
    /// </summary>
    public async Task StopAsync()
    {
        // Try graceful exit via automation
        if (Channel?.IsConnected == true)
        {
            try
            {
                await Channel.SendCommandAsync(Communication.AutomationCommand.Action("Exit"));
                await Channel.DisconnectAsync();
            }
            catch
            {
                // Ignore errors during shutdown
            }
        }

        // Wait for clean exit
        if (_gameProcess != null && !_gameProcess.HasExited)
        {
            var exited = _gameProcess.WaitForExit(5000);
            if (!exited)
            {
                // Force kill
                try
                {
                    _gameProcess.Kill();
                }
                catch
                {
                    // Process may have exited
                }
            }
        }

        _gameProcess?.Dispose();
        _gameProcess = null;
        Channel = null;
    }

    private async Task StartGameProcessAsync(CancellationToken cancellationToken)
    {
        var gamePath = _options.GameExecutablePath
            ?? throw new InvalidOperationException("GameExecutablePath must be specified when not attaching to existing process");

        if (!File.Exists(gamePath))
        {
            throw new FileNotFoundException($"Game executable not found: {gamePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = gamePath,
            Arguments = string.Join(" ", _options.GameArguments),
            UseShellExecute = false
        };

        _gameProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start game process");

        // Wait for window to appear
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < _options.StartupTimeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_gameProcess.HasExited)
            {
                throw new InvalidOperationException($"Game process exited unexpectedly with code {_gameProcess.ExitCode}");
            }

            _gameProcess.Refresh();
            if (_gameProcess.MainWindowHandle != IntPtr.Zero)
            {
                break;
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    private async Task ConnectToAutomationAsync(CancellationToken cancellationToken)
    {
        Channel = new Communication.NamedPipeChannel(_options.PipeName);

        var timeout = TimeSpan.FromMilliseconds(_options.ConnectionTimeoutMs);
        await Channel.ConnectAsync(timeout, cancellationToken);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
