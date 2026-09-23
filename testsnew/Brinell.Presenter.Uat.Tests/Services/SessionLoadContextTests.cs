using Brinell.Presenter.Services;
using Brinell.Uat;

namespace Brinell.Presenter.Uat.Tests.Services;

/// <summary>
/// A run must not make its own workspace unbuildable. The default load context maps the Pages
/// assembly for the life of the process, so the session uses a collectible one and releases the
/// file when it ends - which is what makes "run, edit a page object, build, run again" possible
/// without restarting Presenter.
/// </summary>
public sealed class SessionLoadContextTests
{
    [Fact(Timeout = 120000)]
    public async Task Session_ReleasesThePagesAssemblyWhenItEnds()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellSessionUnload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        // A copy under a different file name: LoadRequired short-circuits to an assembly the
        // host already has when the names match, and then nothing would be loaded to unload.
        var pages = Path.Combine(root, "Brinell.Probe.Pages.dll");
        File.Copy(typeof(SessionLoadContextTests).Assembly.Location, pages);

        try
        {
            File.WriteAllText(
                Path.Combine(root, "uat.config.md"),
                $$"""
                # UAT Config

                ## Runtime

                | Field | Value |
                | --- | --- |
                | Target | WPF |
                | Fixture | UnloadProbeFixture |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | {{Path.GetFileName(pages)}} |
                """);

            var scenarioPath = Path.Combine(root, "unload.uat.md");
            File.WriteAllText(
                scenarioPath,
                """
                # UAT: Unload

                ## Scenario: Session loads the pages assembly

                Given the probe is ready
                """);

            Assert.True(CanOpenForWrite(pages), "The copy should be writable before a session loads it.");

            var session = await new UatExecutionService().CreateSessionAsync(
                root,
                scenarioPath,
                "Session loads the pages assembly",
                CancellationToken.None);

            // Teeth: the file must actually be mapped by the run, or the release below proves nothing.
            Assert.False(
                CanOpenForWrite(pages),
                "The session did not load the Pages assembly, so this test would pass for the wrong reason.");

            session.Dispose();

            Assert.True(
                CanOpenForWrite(pages),
                "The Pages assembly is still locked after the session ended, so the next build would fail.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static bool CanOpenForWrite(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Write, FileShare.None);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A mapped image refuses a write handle; which of the two it throws is not the point.
            return false;
        }
    }

    private static void TryDelete(string root)
    {
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Unload completes asynchronously; a leftover temp folder is not worth failing over.
        }
    }
}

/// <summary>A fixture with nothing to launch, so the test measures loading and nothing else.</summary>
public sealed class UnloadProbeFixture
{
    /// <summary>The one phrase the probe scenario uses.</summary>
    [UatPhrase(Brinell.Core.Testing.UatEffectiveStepKeyword.Given, "the probe is ready")]
    public bool Ready() => true;
}
