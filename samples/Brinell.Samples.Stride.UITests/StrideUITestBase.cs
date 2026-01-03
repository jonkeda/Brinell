using System.Diagnostics;
using Brinell.Core.Logging;
using Brinell.Stride.Communication;
using Brinell.Stride.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests;

/// <summary>
/// Base class for Stride UI tests with app lifecycle management.
/// </summary>
public abstract class StrideUITestBase : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private Process? _gameProcess;
    protected StrideTestContext Context { get; private set; } = null!;

    /// <summary>
    /// Path to the sample app executable.
    /// </summary>
    protected virtual string AppPath => GetAppPath();

    /// <summary>
    /// Test options.
    /// </summary>
    protected virtual StrideTestOptions Options => new()
    {
        DefaultTimeoutMs = 10000,
        ShortTimeoutMs = 2000,
        PollingIntervalMs = 100,
        GameExecutablePath = AppPath,
        GameArguments = ["--automation"],
        EnableCsvLogging = true
    };

    protected StrideUITestBase(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var options = Options;
        
        _output.WriteLine($"Starting test: {GetType().Name}");
        _output.WriteLine($"App path: {options.GameExecutablePath}");

        // Kill any stale processes from previous test runs
        await KillStaleProcessesAsync();

        // Start the game process
        if (!string.IsNullOrEmpty(options.GameExecutablePath) && File.Exists(options.GameExecutablePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = options.GameExecutablePath,
                Arguments = string.Join(" ", options.GameArguments),
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = false
            };
            _gameProcess = Process.Start(startInfo);
            _output.WriteLine($"Game process started: PID {_gameProcess?.Id}");
            
            // IMPORTANT: Must read both stdout and stderr asynchronously to prevent buffer blocking
            if (_gameProcess != null)
            {
                _gameProcess.OutputDataReceived += (s, e) => 
                {
                    // Consume stdout to prevent buffer from filling and blocking the process
                    // Only log important messages to avoid noise
                    if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("Error"))
                        _output.WriteLine($"[GAME] {e.Data}");
                };
                _gameProcess.ErrorDataReceived += (s, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        _output.WriteLine($"[GAME ERR] {e.Data}");
                };
                _gameProcess.BeginOutputReadLine();
                _gameProcess.BeginErrorReadLine();
            }
        }

        // Give game time to start and create window
        await Task.Delay(3000);

        // Verify game is still running
        if (_gameProcess == null || _gameProcess.HasExited)
        {
            var exitCode = _gameProcess?.ExitCode ?? -1;
            throw new InvalidOperationException($"Game process exited immediately with code {exitCode}");
        }

        // Connect to game via named pipe with retry
        var channel = new NamedPipeChannel(options.PipeName);
        var connected = false;
        var retries = 5;
        
        while (!connected && retries > 0)
        {
            try
            {
                await channel.ConnectAsync(TimeSpan.FromMilliseconds(options.ConnectionTimeoutMs / 5));
                connected = true;
            }
            catch
            {
                retries--;
                if (retries > 0)
                {
                    _output.WriteLine($"Connection failed, retrying... ({retries} attempts left)");
                    await Task.Delay(500);
                }
            }
        }

        if (!connected)
        {
            throw new InvalidOperationException("Failed to connect to game automation server");
        }
        
        Context = new StrideTestContext(channel, options);
        Context.TestName = GetType().Name;

        // Wait for window to be created and get its handle
        if (_gameProcess != null)
        {
            var windowReady = false;
            for (int i = 0; i < 20; i++) // Try for 2 seconds
            {
                _gameProcess.Refresh();
                if (_gameProcess.MainWindowHandle != IntPtr.Zero)
                {
                    Context.SetGameWindowHandle(_gameProcess.MainWindowHandle);
                    _output.WriteLine($"Game window handle: {_gameProcess.MainWindowHandle:X}");
                    windowReady = true;
                    break;
                }
                await Task.Delay(100);
            }

            if (!windowReady)
            {
                _output.WriteLine("Warning: Could not get game window handle");
            }
        }

        _output.WriteLine("Connected to game automation");
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        try
        {
            // Release any stuck modifier keys before cleanup
            try
            {
                Context?.Input?.ReleaseAllModifiers();
            }
            catch
            {
                // Ignore errors releasing keys
            }

            // Dispose context first to close pipe gracefully
            Context?.Dispose();
            
            // Give process time to notice pipe closed
            await Task.Delay(100);
            
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                // Try graceful close first
                _gameProcess.CloseMainWindow();
                
                // Wait briefly for graceful shutdown
                var exited = _gameProcess.WaitForExit(1000);
                
                if (!exited)
                {
                    // Force kill if still running
                    _gameProcess.Kill();
                    await _gameProcess.WaitForExitAsync();
                }
                
                _output.WriteLine($"Game process stopped (exit code: {_gameProcess.ExitCode})");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error stopping game: {ex.Message}");
        }
        finally
        {
            _gameProcess?.Dispose();
            _gameProcess = null;
        }
    }

    /// <summary>
    /// Kill any stale game processes from previous runs.
    /// </summary>
    private async Task KillStaleProcessesAsync()
    {
        try
        {
            var processName = Path.GetFileNameWithoutExtension(AppPath);
            var staleProcesses = Process.GetProcessesByName(processName);
            
            foreach (var proc in staleProcesses)
            {
                try
                {
                    _output.WriteLine($"Killing stale process: {proc.Id}");
                    proc.Kill();
                    await proc.WaitForExitAsync();
                }
                catch
                {
                    // Ignore errors killing stale processes
                }
            }
        }
        catch
        {
            // Ignore errors finding processes
        }
    }

    /// <summary>
    /// Log a message to test output.
    /// </summary>
    protected void Log(string message)
    {
        _output.WriteLine(message);
        Context?.Log(message);
    }

    private static string GetAppPath()
    {
        // Calculate path relative to test assembly
        var assemblyDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));
        var appDir = Path.Combine(solutionDir, "samples", "Brinell.Samples.Stride.App");
        
        // Determine build configuration
        var config = assemblyDir.Contains("Release") ? "Release" : "Debug";
        
        return Path.Combine(appDir, "bin", config, "net10.0-windows", "Brinell.Samples.Stride.App.exe");
    }
}
