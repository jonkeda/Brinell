using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Brinell.Html.Testing;

/// <summary>How to start a web app under test.</summary>
public sealed record WebAppHostOptions
{
    /// <summary>The app: a built <c>.dll</c> (run with <c>dotnet app.dll</c>) or a <c>.csproj</c> (run with <c>dotnet run</c>).</summary>
    public required string AppPath { get; init; }

    /// <summary>Environment variables for the app's process: its launch settings.</summary>
    public IReadOnlyDictionary<string, string> Environment { get; init; } = new Dictionary<string, string>();

    /// <summary>The path that answers 2xx once the app is up.</summary>
    public string ReadyPath { get; init; } = "/";

    /// <summary>How long the app may take to answer <see cref="ReadyPath"/>.</summary>
    public TimeSpan StartTimeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>A name for messages, when several apps run at once.</summary>
    public string? Name { get; init; }
}

/// <summary>
/// A web app under test, started in its own process on a free port. Dispose to stop it.
/// </summary>
/// <remarks>
/// One process is one app instance, with its own port and launch settings; start two for two
/// instances (<c>.my/html/todo-blazor.md</c> 4.2: hosts start on a free port with explicit launch
/// settings, never at a fixed URL).
/// </remarks>
public sealed class WebAppHost : IDisposable
{
    private static readonly TimeSpan ReadyPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly Process _process;
    private readonly List<string> _output = [];
    private readonly object _outputGate = new();
    private bool _disposed;

    private WebAppHost(Process process, string baseUrl, string name)
    {
        _process = process;
        BaseUrl = baseUrl;
        Name = name;
    }

    /// <summary>Where the app listens, without a trailing slash.</summary>
    public string BaseUrl { get; }

    /// <summary>The app's name, for messages.</summary>
    public string Name { get; }

    /// <summary>Starts the app and waits until it answers.</summary>
    /// <exception cref="InvalidOperationException">The app exited during startup; the message has its output.</exception>
    /// <exception cref="TimeoutException">The app did not answer in time; the message has its output.</exception>
    public static WebAppHost Start(WebAppHostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var appPath = Path.GetFullPath(options.AppPath);
        if (!File.Exists(appPath))
        {
            throw new FileNotFoundException($"The app to start was not found at {appPath}. Build it first.", appPath);
        }

        var baseUrl = $"http://127.0.0.1:{FreePort()}";
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(appPath)!,
        };

        if (appPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var argument in new[] { "run", "--project", appPath, "--no-launch-profile" })
            {
                startInfo.ArgumentList.Add(argument);
            }
        }
        else
        {
            startInfo.ArgumentList.Add(appPath);
        }

        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseUrl);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        foreach (var (name, value) in options.Environment)
        {
            startInfo.Environment[name] = value;
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start {appPath}.");
        var host = new WebAppHost(process, baseUrl, options.Name ?? Path.GetFileNameWithoutExtension(appPath));
        process.OutputDataReceived += (_, args) => host.Capture(args.Data);
        process.ErrorDataReceived += (_, args) => host.Capture(args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var started = false;
        try
        {
            host.WaitUntilAnswering(options.ReadyPath, options.StartTimeout);
            started = true;
        }
        finally
        {
            if (!started)
            {
                host.Dispose();
            }
        }

        return host;
    }

    /// <summary>Stops the app.</summary>
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

    // A host startup loop, the one kind of wait AGENTS.md allows outside a call: there is no UI yet.
    private void WaitUntilAnswering(string readyPath, TimeSpan timeout)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var url = $"{BaseUrl}/{readyPath.TrimStart('/')}";
        var clock = Stopwatch.StartNew();

        while (clock.Elapsed < timeout)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"{Name} exited with code {_process.ExitCode} while starting.{System.Environment.NewLine}{Output()}");
            }

            if (Answers(client, url))
            {
                return;
            }

            Thread.Sleep(ReadyPollInterval);
        }

        throw new TimeoutException(
            $"{Name} did not answer {url} within {timeout.TotalSeconds:0} s.{System.Environment.NewLine}{Output()}");
    }

    /// <summary>Whether <paramref name="url"/> answers 2xx now. A refused or slow connection is a "no".</summary>
    private static bool Answers(HttpClient client, string url)
    {
        try
        {
            using var response = client.GetAsync(url).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private void Capture(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            lock (_outputGate)
            {
                _output.Add(line);
            }
        }
    }

    private string Output()
    {
        lock (_outputGate)
        {
            return string.Join(System.Environment.NewLine, _output.TakeLast(40));
        }
    }
}
