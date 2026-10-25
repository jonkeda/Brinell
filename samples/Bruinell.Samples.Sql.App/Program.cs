using Bruinell.Samples.Sql.App.Components;
using Bruinell.Samples.Sql.App.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
// Spike:Connection lets a test hosting the app in its own process give each host its own database.
builder.Services.AddSpikeData(builder.Configuration["Spike:Connection"] ?? SpikeData.Connection);

var app = builder.Build();

// The schema, not the data: the test fixture seeds the places. A test hosting the app in its own process
// (WebApplicationFactory) sets Spike:Migrate to false: its fixture migrates, and in replay there is no database.
if (app.Configuration.GetValue("Spike:Migrate", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpikeDbContext>>().CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok("healthy"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

/// <summary>The app's entry point, public for a test that hosts it in its own process.</summary>
public partial class Program;
