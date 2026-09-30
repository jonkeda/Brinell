using Brinell.Presenter.Services;
using Brinell.Uat;

namespace Brinell.Presenter.Bdd.Tests.Services;

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

            var scenarioPath = Path.Combine(root, "unload.feature.md");
            File.WriteAllText(
                scenarioPath,
                """
                # Feature: Unload

                ## Scenario: Session loads the pages assembly

                ```gherkin
                Given the probe is ready
                ```
                """);

            Assert.True(CanOpenForWrite(pages), "The copy should be writable before a session loads it.");

            var session = await new BddExecutionService().CreateSessionAsync(
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

    /// <summary>
    /// An assembly built here (its symbols beside it) is read into memory, so its file is free
    /// for the next build even while the session runs, and even when the context cannot unload.
    /// </summary>
    [Fact(Timeout = 120000)]
    public async Task Session_NeverLocksAPagesAssemblyThatHasSymbols()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellSessionUnload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = typeof(SessionLoadContextTests).Assembly.Location;
        var pages = Path.Combine(root, "Brinell.Probe.Pages.dll");
        File.Copy(source, pages);
        File.Copy(Path.ChangeExtension(source, ".pdb"), Path.ChangeExtension(pages, ".pdb"));

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
                | Fixture | LocationProbeFixture |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | {{Path.GetFileName(pages)}} |
                """);

            var scenarioPath = Path.Combine(root, "unload.feature.md");
            File.WriteAllText(
                scenarioPath,
                $$"""
                # Feature: Unload

                ## Scenario: Session loads the pages assembly

                ```gherkin
                Given the probe was built into "{{root}}"
                ```
                """);

            var session = await new BddExecutionService().CreateSessionAsync(
                root,
                scenarioPath,
                "Session loads the pages assembly",
                CancellationToken.None);
            try
            {
                Assert.True(CanOpenForWrite(pages), "The Pages assembly is locked while the session runs.");

                // The suite still finds its folder: AssemblyFile has the path the file was read from.
                var result = await session.RunNextAsync(CancellationToken.None);
                Assert.True(result.Status == BddStepResultStatus.Passed, result.Message);
            }
            finally
            {
                session.Dispose();
            }
        }
        finally
        {
            TryDelete(root);
        }
    }

    [Fact(Timeout = 120000)]
    public async Task Session_ReleasesThePagesAssemblyWhenItFailsToStart()
    {
        var root = Path.Combine(Path.GetTempPath(), "BrinellSessionUnload", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
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
                | Fixture | FailingProbeFixture |

                ## Assemblies

                | Kind | Assembly |
                | --- | --- |
                | Pages | {{Path.GetFileName(pages)}} |
                """);

            var scenarioPath = Path.Combine(root, "unload.feature.md");
            File.WriteAllText(
                scenarioPath,
                """
                # Feature: Unload

                ## Scenario: Session fails to start

                ```gherkin
                Given the probe is ready
                ```
                """);

            var message = await StartFailureAsync(root, scenarioPath);

            Assert.Contains(FailingProbeFixture.Reason, message, StringComparison.Ordinal);
            Assert.True(
                CanOpenForWrite(pages),
                "The Pages assembly is still locked after the session failed to start, so the next build would fail.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    // Only the message leaves: a test that held the exception would hold the suite's types itself.
    private static async Task<string> StartFailureAsync(string root, string scenarioPath)
    {
        var failure = await Assert.ThrowsAnyAsync<Exception>(() => new BddExecutionService().CreateSessionAsync(
            root,
            scenarioPath,
            "Session fails to start",
            CancellationToken.None));
        return failure.Message;
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
    [BddGiven("the probe is ready")]
    public bool Ready() => true;
}

/// <summary>A fixture whose type cannot be initialized, as one that reads a missing file when it loads.</summary>
public sealed class FailingProbeFixture
{
    /// <summary>What the failure says.</summary>
    public const string Reason = "The probe fixture cannot start.";

    private static readonly string Setting;

    static FailingProbeFixture() => Setting = Fail();

    /// <summary>What the fixture read when it loaded.</summary>
    public string Read => Setting;

    private static string Fail() => throw new InvalidOperationException(Reason);
}

/// <summary>A fixture that says where its assembly was built, as a suite that looks for its files does.</summary>
public sealed class LocationProbeFixture
{
    /// <summary>Passes when <c>AssemblyFile</c> gives the folder the assembly was read from.</summary>
    /// <param name="folder">The folder expected.</param>
    [BddGiven("the probe was built into {folder}")]
    public void BuiltInto(string folder)
        => Assert.Equal(folder, Brinell.Core.Composition.AssemblyFile.FolderOf(typeof(LocationProbeFixture).Assembly));
}
