using Brinell.Samples.Sql.App.Components;
using Brinell.Samples.Sql.App.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSpikeData(SpikeData.Connection);

var app = builder.Build();

// The schema, not the data: the test fixture seeds the places.
await using (var scope = app.Services.CreateAsyncScope())
{
    await using var db = await scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpikeDbContext>>().CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok("healthy"));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
