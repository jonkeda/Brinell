using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Brinell.Core.Configuration;

namespace Brinell.Blazor.Uat.Tests.Runtime;

internal sealed class BlazorSampleHost : IDisposable
{
    private readonly Process _process;
    private readonly List<string> _output = [];
    private readonly BlazorOptions _options;
    private bool _disposed;

    private BlazorSampleHost(Process process, string baseUrl, BlazorOptions options)
    {
        _process = process;
        BaseUrl = baseUrl;
        _options = options;
    }

    public string BaseUrl { get; }

    public static BlazorSampleHost Start(BlazorOptions? options = null)
    {
        options ??= BrinellBlazorConfiguration.Load()?.Blazor ?? new BlazorOptions();
        
        var projectPath = ResolveAppProjectPath(options);
        var port = GetFreePort();
        var baseUrl = $"http://127.0.0.1:{port}";

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-launch-profile");
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseUrl);
        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Development";

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Blazor sample app.");

        try
        {
            var host = new BlazorSampleHost(process, baseUrl, options);
            process.OutputDataReceived += (_, args) => host.Capture(args.Data);
            process.ErrorDataReceived += (_, args) => host.Capture(args.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            host.WaitUntilReadyAsync().GetAwaiter().GetResult();
            return host;
        }
        catch
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }

            process.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(5000);
        }

        _process.Dispose();
    }

    private async Task WaitUntilReadyAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
        var readyTimeoutSeconds = _options.ReadyTimeoutSeconds > 0 ? _options.ReadyTimeoutSeconds : 90;
        var deadline = DateTime.UtcNow.AddSeconds(readyTimeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Blazor sample app exited with code {_process.ExitCode}.{Environment.NewLine}{FormatOutput()}");
            }

            try
            {
                using var response = await client.GetAsync(BaseUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(100).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Blazor sample app did not become HTTP-ready at {BaseUrl}.{Environment.NewLine}{FormatOutput()}");
    }

    private void Capture(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            _output.Add(line);
        }
    }

    private string FormatOutput()
        => _output.Count == 0 ? "(no process output)" : string.Join(Environment.NewLine, _output);

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string ResolveAppProjectPath(BlazorOptions options)
    {
        // Use configuration path if available
        if (!string.IsNullOrWhiteSpace(options.AppPath))
        {
            return Path.GetFullPath(options.AppPath);
        }

        // Fall back to default location
        var solutionDir = FindSolutionDirectory();
        return Path.Combine(solutionDir, "samples", "Brinell.Samples.Blazor.App", "Brinell.Samples.Blazor.App.csproj");
    }

    private static string FindSolutionDirectory()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(dir))
        {
            if (File.Exists(Path.Combine(dir, "Brinell.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate Brinell.sln.");
    }
}
