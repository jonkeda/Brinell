namespace Brinell.Stride.UITests;

/// <summary>
/// Shared fixture that manages game lifecycle for all tests in the collection.
/// xUnit calls InitializeAsync/DisposeAsync via IAsyncLifetime to start/stop the game.
/// </summary>
public class StrideAppFixture : StrideTestFixtureBase, IAsyncLifetime
{
    protected override string GetDefaultAppPath()
    {
        var assemblyDir = AppContext.BaseDirectory;
        var solutionDir = Path.GetFullPath(Path.Combine(assemblyDir, "..", "..", "..", "..", ".."));
        return Path.Combine(solutionDir, "samples", "Brinell.Samples.Stride.App", "bin", "Debug", "net10.0-windows", "Brinell.Samples.Stride.App.exe");
    }

    async Task IAsyncLifetime.InitializeAsync() => await base.InitializeAsync();
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

/// <summary>
/// Collection definition that shares a single StrideAppFixture across all test classes.
/// This ensures only one game process is launched for the entire test run.
/// </summary>
[CollectionDefinition("Stride")]
public class StrideCollection : ICollectionFixture<StrideAppFixture>;

/// <summary>
/// Base class for Stride UI tests. Uses shared fixture for game lifecycle.
/// </summary>
[Collection("Stride")]
public abstract class StrideUITestBase
{
    protected readonly IStrideTestContext Context;
    protected readonly ITestOutputHelper Output;

    protected StrideUITestBase(StrideAppFixture fixture, ITestOutputHelper output)
    {
        Context = fixture.Context;
        Output = output;
    }

    protected void Log(string message) => Output.WriteLine(message);
}
