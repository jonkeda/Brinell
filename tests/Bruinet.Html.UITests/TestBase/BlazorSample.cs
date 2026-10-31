using Bruinet.Html.Testing;

namespace Bruinet.Html.UITests.TestBase;

/// <summary>
/// The Blazor gallery sample, started once for the test run on a free port, and stopped when the
/// run ends. Set <c>BLAZOR_APP_URL</c> to use one that is already running instead.
/// </summary>
internal static class BlazorSample
{
    private static readonly Lazy<string> Url = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Where the sample listens, without a trailing slash.</summary>
    public static string BaseUrl => Url.Value;

    private static string Start()
    {
        if (Environment.GetEnvironmentVariable("BLAZOR_APP_URL") is { Length: > 0 } running)
        {
            return running.TrimEnd('/');
        }

        var host = WebAppHost.Start(new WebAppHostOptions { AppPath = WebAppHost.FindBuiltApp("samples/Bruinet.Samples.Blazor.App"), Name = "Blazor sample" });
        AppDomain.CurrentDomain.ProcessExit += (_, _) => host.Dispose();
        return host.BaseUrl;
    }
}
