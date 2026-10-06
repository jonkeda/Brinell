using Brinell.Core.Composition;
using Brinell.Maui.Enums;
using Brinell.Maui.Interfaces;
using Brinell.Maui.Testing;
using Brinell.Samples.Todo.TestSupport.Uat;
using Brinell.Samples.Todo.Uat.Phrases;
using Brinell.Samples.Todo.UITests;
using Brinell.Samples.Todo.UITests.Pages;
using Brinell.Uat;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Samples.Todo.Uat.Runtime;

/// <summary>
/// The Todo app, hermetic, presented to the UAT engine as its root. It starts on the
/// <c>DefaultTestData</c> of <c>uat.config.md</c>; a scenario file that names another test set
/// switches to it (<see cref="TodoUatScenarioTests"/>).
/// </summary>
/// <remarks>
/// The base fixture launches the app and stands up the WireMock backend; this adds the two things
/// the UAT runtime needs. <see cref="Composition"/> is where it discovers the page objects and
/// phrases (from <c>[TestModuleScan]</c>) and how it constructs them (the scope resolves
/// <see cref="IMauiTestContext"/>). It is rebuilt after a restart, when the context is new.
/// </remarks>
[TestModuleScan(typeof(TodoListPage), NamespacePrefix = "Brinell.Samples.Todo.UITests.Pages")]
[TestModuleScan(typeof(AppLifecyclePhrases), NamespacePrefix = "Brinell.Samples.Todo.Uat.Phrases")]
[TestModuleScan(typeof(BackendPhrases), NamespacePrefix = "Brinell.Samples.Todo.TestSupport.Uat")]
[UatPlatforms(MauiPlatform.Windows, MauiPlatform.Android)]
public sealed class TodoUatFixture : TodoAppFixture, IUatScenarioLifecycle
{
    private IServiceScope? _restartScope;

    /// <summary>Builds the composition the UAT runtime reads for page discovery and construction.</summary>
    public TodoUatFixture()
    {
        Composition = CreateComposition();
    }

    /// <summary>The test set a scenario file runs on when its metadata names none: <c>DefaultTestData</c> in <c>uat.config.md</c>.</summary>
    public static string DefaultTestData { get; } = ReadDefaultTestData();

    /// <inheritdoc />
    protected override string TestDataName => DefaultTestData;

    /// <summary>The page graph and DI scope the UAT runtime resolves controls through.</summary>
    public TestComposition Composition { get; private set; }

    /// <inheritdoc />
    protected override void OnAppRestarted() => Composition = CreateComposition();

    /// <inheritdoc />
    /// <remarks>The file's <c>TestData</c>, or <see cref="DefaultTestData"/>; the app restarts when it changes.</remarks>
    public void BeforeFile(BddDocument document)
        => UseTestSet(document.Metadata.TryGetValue(BddMetadataFields.TestData, out var name) && name.Length > 0
            ? name
            : DefaultTestData);

    /// <inheritdoc />
    public void BeforeScenario(BddBoundScenario scenario) => StartTest();

    /// <summary>
    /// Points a running scenario at the restarted app: its own scope still resolves pages against
    /// the context the restart closed.
    /// </summary>
    /// <param name="context">The scenario's execution context.</param>
    public void RescopeAfterRestart(BddExecutionContext context)
    {
        _restartScope?.Dispose();
        _restartScope = Composition.CreateScope();
        UatRuntime.ConfigureScope(context, _restartScope.ServiceProvider);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _restartScope?.Dispose();
        }

        base.Dispose(disposing);
    }

    private TestComposition CreateComposition()
        => TestComposition.ForFixture(this, services => services.AddSingleton<IMauiTestContext>(Context));

    private static string ReadDefaultTestData()
    {
        // Beside this assembly, wherever the suite runs (AD-025); the source's when a host runs it from there.
        var config = BddConfigParser.ParseFile(BddScenarioSource.GetConfigFilePath(BddScenarioSource.SuiteDirectory(AssemblyFile.FolderOf(typeof(TodoUatFixture).Assembly))));
        return config.Runtime.TryGetValue(BddConfigFields.DefaultTestData, out var name) && name.Length > 0
            ? name
            : throw new InvalidOperationException($"uat.config.md has no '{BddConfigFields.DefaultTestData}' in its Runtime table.");
    }
}
