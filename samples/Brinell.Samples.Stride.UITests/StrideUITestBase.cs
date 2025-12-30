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

        // Start the game process
        if (!string.IsNullOrEmpty(options.GameExecutablePath) && File.Exists(options.GameExecutablePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = options.GameExecutablePath,
                Arguments = string.Join(" ", options.GameArguments),
                UseShellExecute = false
            };
            _gameProcess = Process.Start(startInfo);
            _output.WriteLine($"Game process started: PID {_gameProcess?.Id}");
        }

        // Give game time to start
        await Task.Delay(2000);

        // Connect to game via named pipe
        var channel = new NamedPipeChannel(options.PipeName);
        await channel.ConnectAsync(TimeSpan.FromMilliseconds(options.ConnectionTimeoutMs));
        
        Context = new StrideTestContext(channel, options);
        Context.TestName = GetType().Name;

        _output.WriteLine("Connected to game automation");
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        try
        {
            Context?.Dispose();
            
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                _gameProcess.Kill();
                await _gameProcess.WaitForExitAsync();
                _output.WriteLine("Game process stopped");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error stopping game: {ex.Message}");
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
