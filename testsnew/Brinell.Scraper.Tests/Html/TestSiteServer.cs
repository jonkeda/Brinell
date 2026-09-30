using System.Net;
using System.Net.Sockets;

namespace Brinell.Scraper.Tests.Html;

/// <summary>
/// Serves the test site (copied beside the tests) on a free port, on <c>localhost</c> and on
/// <c>127.0.0.1</c>: two origins, for cross-origin frames.
/// </summary>
public sealed class TestSiteServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "TestSite");

    public TestSiteServer()
    {
        Port = FreePort();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        _listener.Start();
        _ = Task.Run(ServeAsync);
    }

    public int Port { get; }

    /// <summary>A page's address on the first origin.</summary>
    public string Url(string path) => $"http://localhost:{Port}/{path}";

    /// <summary>A page's address on the second origin.</summary>
    public string OtherOriginUrl(string path) => $"http://127.0.0.1:{Port}/{path}";

    public void Dispose() => _listener.Close();

    private async Task ServeAsync()
    {
        while (_listener.IsListening)
        {
            var context = await TryNextAsync();
            if (context is null) return;
            var file = Path.Combine(_root, context.Request.Url!.AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(file))
            {
                context.Response.ContentType = Path.GetExtension(file) switch
                {
                    ".html" => "text/html; charset=utf-8",
                    ".css" => "text/css",
                    ".js" => "text/javascript",
                    _ => "application/octet-stream",
                };
                await context.Response.OutputStream.WriteAsync(await File.ReadAllBytesAsync(file));
            }
            else
            {
                context.Response.StatusCode = 404;
            }
            context.Response.Close();
        }
    }

    private async Task<HttpListenerContext?> TryNextAsync()
    {
        try
        {
            return await _listener.GetContextAsync();
        }
        catch (Exception e) when (e is HttpListenerException or ObjectDisposedException)
        {
            return null; // stopped
        }
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
