using System.Runtime.CompilerServices;
using Brinell.Core.Composition;
using Brinell.Core.Testing;
using Xunit;

namespace Brinell.Uat.Workspace.Tests;

/// <summary>A suite loaded the way every tool loads it: from a shadow copy, unloadable (AD-018).</summary>
public sealed class UatSuiteTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Brinell", "UatSuiteTests", Guid.NewGuid().ToString("N"));
    private readonly string _built;

    public UatSuiteTests()
    {
        // The suite's build output: this test assembly, alone in a bin folder of its own, so the
        // tests can overwrite it as a build would.
        var bin = Path.Combine(_folder, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(bin);
        _built = Path.Combine(bin, "Shelf.Steps.dll");
        File.Copy(typeof(UatSuiteTests).Assembly.Location, _built);
        File.WriteAllText(Path.Combine(_folder, "bdd.config.md"), $"""
            # BDD Config

            ## Runtime

            | Field | Value |
            | --- | --- |
            | Fixture | {nameof(ShelfFixture)} |
            | Head | service |

            ## Assemblies

            | Kind | Assembly |
            | --- | --- |
            | Steps | bin/Debug/net10.0/Shelf.Steps.dll |
            """);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
            // A context not collected yet; the temp folder is cleaned by the OS.
        }
    }

    [Fact]
    public void Catalogs_ComeFromTheFixture_InTheBuildOutput()
    {
        using var suite = UatSuite.Open(Path.Combine(_folder, "bdd.config.md"));

        Assert.True(suite.CanBind);
        Assert.Equal("service", suite.Head);
        Assert.Contains(suite.Catalogs.Default.Patterns, pattern => pattern.Phrase == "I shelve {title}");
    }

    [Fact]
    public void TheBuildOutput_CanBeOverwritten_WhileTheCatalogsAreHeld()
    {
        using var suite = UatSuite.Open(Path.Combine(_folder, "bdd.config.md"));
        var catalogs = suite.Catalogs;

        // What a build does to its output: replace the assembly.
        File.Copy(typeof(UatSuiteTests).Assembly.Location, _built, overwrite: true);
        File.Delete(_built);

        Assert.NotEmpty(catalogs.Default.Patterns);
    }

    [Fact]
    public void Reload_UnloadsTheAssemblies_AndTheNextCatalogsAreNew()
    {
        using var suite = UatSuite.Open(Path.Combine(_folder, "bdd.config.md"));
        var context = LoadAndForget(suite);

        suite.Reload();
        for (var attempt = 0; attempt < 10 && context.IsAlive; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(context.IsAlive, "The suite's load context was not collected after Reload.");
        Assert.Contains(suite.Catalogs.Default.Patterns, pattern => pattern.Phrase == "I shelve {title}");
    }

    [Fact]
    public void Catalogs_BeforeABuild_SayToBuildFirst()
    {
        File.Delete(_built);
        using var suite = UatSuite.Open(Path.Combine(_folder, "bdd.config.md"));

        Assert.Null(suite.TryGetCatalogs(out var reason));
        Assert.Contains("Build the UAT project first", reason);
    }

    // Kept out of the test method, so no local there keeps the context alive.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadAndForget(UatSuite suite)
    {
        _ = suite.Catalogs;
        return suite.LoadContext!;
    }
}

public sealed class ShelfFixture
{
    public TestComposition Composition => throw new NotSupportedException("The workspace reads the fixture's type only.");
}

[BddStepDefinitions]
public sealed class ShelfSteps : BddStepDefinitionsBase
{
    [BddWhen("I shelve {title}")]
    public void Shelve(string title)
    {
    }
}
