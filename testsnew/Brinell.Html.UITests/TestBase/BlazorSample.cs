using Brinell.Html.Testing;

namespace Brinell.Html.UITests.TestBase;

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

        var host = WebAppHost.Start(new WebAppHostOptions { AppPath = SampleAssembly(), Name = "Blazor sample" });
        AppDomain.CurrentDomain.ProcessExit += (_, _) => host.Dispose();
        return host.BaseUrl;
    }

    /// <summary>The sample's built assembly; the project reference in the .csproj builds it with the tests.</summary>
    private static string SampleAssembly()
    {
        const string project = "Brinell.Samples.Blazor.App";
#if DEBUG
        const string configuration = "Debug";
#else
        const string configuration = "Release";
#endif
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "samples", project, "bin", configuration, "net10.0", project + ".dll");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"{project}.dll was not found above {AppContext.BaseDirectory}. Build the sample first.");
    }
}
