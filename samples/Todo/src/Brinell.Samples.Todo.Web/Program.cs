using Brinell.Samples.Todo.Core.Services;
using Brinell.Samples.Todo.Infrastructure;
using Brinell.Samples.Todo.Web.Components;
using Brinell.Samples.Todo.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// One host process is one device: it has its own SQLite file and its own launch settings, read
// from the environment the way the MAUI app reads them on Windows. A test runs two devices by
// starting two hosts. A normal run sets nothing and gets a file under local app data and the
// development API.
var settings = LaunchSettings.From(
    Environment.GetEnvironmentVariable,
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Brinell.Samples.Todo.Web", "todo.db"));

// No device connectivity on a server: only the network-state file can take this head offline.
builder.Services.AddTodoServices(settings);
builder.Services.AddSingleton<WebNavigator>();
builder.Services.AddSingleton<INavigator>(provider => provider.GetRequiredService<WebNavigator>());
builder.Services.AddSingleton<WebDialogs>();
builder.Services.AddSingleton<IDialogs>(provider => provider.GetRequiredService<WebDialogs>());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

// What a test fixture waits for before it opens the first page.
app.MapGet("/health", () => Results.Ok("healthy"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
