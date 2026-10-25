using Bruinell.Core.Composition;
using Bruinell.Samples.Todo.TestSupport.Uat;

namespace Bruinell.Samples.Todo.IntegrationTests.Service;

/// <summary>
/// The Todo app without its UI, as the root of the <c>service</c> head: the scenarios tagged
/// <c>@service</c> run against its services, a database file and a WireMock backend.
/// </summary>
/// <remarks>
/// Each scenario gets a new <see cref="TodoHost"/> on the file's test set (<see cref="StartTest"/>):
/// headless, a fresh start costs milliseconds, so no scenario sees another's data. The steps that
/// reach past the screen (backend, device, stored data) are the UI heads' own, through
/// <see cref="ITodoArrangement"/>; the steps a UI head says with its screen are in <c>Steps/</c>.
/// </remarks>
[TestModuleScan(typeof(BackendPhrases), NamespacePrefix = "Bruinell.Samples.Todo.TestSupport.Uat")]
[TestModuleScan(typeof(TodoServiceFixture), NamespacePrefix = "Bruinell.Samples.Todo.IntegrationTests.Service.Steps")]
public sealed class TodoServiceFixture : ITodoArrangement, IDisposable
{
    private TodoHost? _host;
    private string _testSet = DefaultTestData;

    /// <summary>Builds the composition the runtime discovers the steps from.</summary>
    public TodoServiceFixture()
    {
        Composition = TestComposition.ForFixture(this);
    }

    /// <summary>The test set a file runs on when its metadata names none.</summary>
    public const string DefaultTestData = "one-todo";

    /// <summary>The step classes and the scope that builds them.</summary>
    public TestComposition Composition { get; }

    /// <inheritdoc />
    public TodoTestSet TestSet => Host.TestSet;

    /// <inheritdoc />
    public TodoServerStub Server => Host.Server;

    /// <inheritdoc />
    public MockApiServer Backend => Host.Mock;

    /// <inheritdoc />
    public NetworkStateFile Network => Host.Network;

    /// <inheritdoc />
    public string DatabasePath => Host.Database.Path;

    internal TodoHost Host => _host ?? throw new InvalidOperationException("No scenario has started: StartTest makes the host.");

    /// <summary>The test set the next scenarios start on.</summary>
    /// <param name="name">A test set, such as <c>one-todo</c>.</param>
    public void UseTestSet(string name) => _testSet = name;

    /// <summary>Starts a new host for the next scenario: a new database from the test set, the server's half on WireMock, online.</summary>
    public void StartTest()
    {
        _host?.Dispose();
        _host = TodoHost.StartAsync(_testSet).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void ResetBackend() => Host.ResetBackend();

    /// <inheritdoc />
    public void Dispose() => _host?.Dispose();
}
