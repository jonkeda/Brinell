using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Brinell.Samples.Stride.UITests.Tests;

/// <summary>
/// Simple test to verify app starts and pipe works.
/// </summary>
public class SimpleAppTest
{
    private readonly ITestOutputHelper _output;

    public SimpleAppTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private static string GetAppPath()
    {
        var assemblyDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));
        return Path.Combine(solutionDir, "samples", "Brinell.Samples.Stride.App", "bin", "Debug", "net10.0-windows", "Brinell.Samples.Stride.App.exe");
    }

    [Fact]
    public async Task App_StartsAndStops_Successfully()
    {
        var appPath = GetAppPath();
        _output.WriteLine($"App path: {appPath}");
        Assert.True(File.Exists(appPath), $"App not found: {appPath}");

        // Start process without redirecting output to avoid buffer blocking
        var startInfo = new ProcessStartInfo
        {
            FileName = appPath,
            Arguments = "--automation",
            UseShellExecute = true  // Don't redirect, avoid buffer issues
        };

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
            Assert.NotNull(process);
            _output.WriteLine($"Started process: {process.Id}");

            // Wait for app to initialize
            await Task.Delay(3000);

            // Check process is still running
            Assert.False(process.HasExited, "Process exited prematurely");
            _output.WriteLine("Process is running");

            // Try to connect to pipe
            using var pipe = new NamedPipeClientStream(".", "Brinell.Stride.Automation", PipeDirection.InOut, PipeOptions.Asynchronous);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await pipe.ConnectAsync(cts.Token);
            _output.WriteLine("Connected to pipe");

            // Send a simple query
            using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);

            var command = """{"type":"GameQuery","method":"IsReady"}""";
            await writer.WriteLineAsync(command);
            _output.WriteLine($"Sent: {command}");

            using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await reader.ReadLineAsync(readCts.Token);
            _output.WriteLine($"Received: {response}");

            Assert.NotNull(response);
            Assert.Contains("success", response, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
                _output.WriteLine($"Process killed, exit code: {process.ExitCode}");
            }
            process?.Dispose();
        }
    }

    [Fact]
    public async Task App_GetElementState_ReturnsValidResponse()
    {
        var appPath = GetAppPath();
        Assert.True(File.Exists(appPath), $"App not found: {appPath}");

        var startInfo = new ProcessStartInfo
        {
            FileName = appPath,
            Arguments = "--automation",
            UseShellExecute = true
        };

        Process? process = null;
        try
        {
            process = Process.Start(startInfo);
            Assert.NotNull(process);
            _output.WriteLine($"Started process: {process.Id}");

            await Task.Delay(3000);
            Assert.False(process.HasExited, "Process exited prematurely");

            using var pipe = new NamedPipeClientStream(".", "Brinell.Stride.Automation", PipeDirection.InOut, PipeOptions.Asynchronous);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await pipe.ConnectAsync(cts.Token);
            _output.WriteLine("Connected to pipe");

            using var writer = new StreamWriter(pipe, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);

            // Query VolumeSlider state
            var command = """{"type":"Query","method":"GetState","target":"VolumeSlider"}""";
            await writer.WriteLineAsync(command);
            _output.WriteLine($"Sent: {command}");

            using var readCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await reader.ReadLineAsync(readCts.Token);
            _output.WriteLine($"Received: {response}");

            Assert.NotNull(response);
            Assert.Contains("success", response, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("VolumeSlider", response);
        }
        finally
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
                _output.WriteLine($"Process killed");
            }
            process?.Dispose();
        }
    }
}
