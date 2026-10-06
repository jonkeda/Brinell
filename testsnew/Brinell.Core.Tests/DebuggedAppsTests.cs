using System.Diagnostics;
using System.Text.Json;
using Brinell.Core.Diagnostics;

namespace Brinell.Core.Tests;

public sealed class DebuggedAppsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Brinell", "DebuggedAppsTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void WithADebugger_TheAppIsWritten_ByTestProcessAndAppProcess()
    {
        using var app = Process.GetCurrentProcess();

        var file = DebuggedApps.Announce(app, "Todo", attached: () => true, folder: _folder);

        Assert.Equal(Path.Combine(_folder, $"{Environment.ProcessId}-{app.Id}.json"), file);
        using var json = JsonDocument.Parse(File.ReadAllText(file!));
        Assert.Equal(app.Id, json.RootElement.GetProperty("app").GetInt32());
        Assert.Equal(Environment.ProcessId, json.RootElement.GetProperty("testProcess").GetInt32());
        Assert.Equal("Todo", json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public void WithoutADebugger_NothingIsWritten()
    {
        using var app = Process.GetCurrentProcess();

        Assert.Null(DebuggedApps.Announce(app, "Todo", attached: () => false, folder: _folder));
        Assert.False(Directory.Exists(_folder));
    }
}
