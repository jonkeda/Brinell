using Bruinet.Scraper.TestSite;

// The Scraper's test site on its own, for the loop (tools/Scripts/scrape-loop.ps1): serves on a free
// port (or --port), says where on the first line, and stops when its input closes or on Ctrl+C.
var port = args.Length == 2 && args[0] == "--port" && int.TryParse(args[1], out var p) ? p : 0;
using var site = new TestSiteServer(port);
Console.WriteLine($"Listening on {site.Url("")}");
var stop = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    stop.TrySetResult();
};
_ = Task.Run(() =>
{
    while (Console.In.ReadLine() is not null)
    {
    }
    stop.TrySetResult();
});
await stop.Task;
