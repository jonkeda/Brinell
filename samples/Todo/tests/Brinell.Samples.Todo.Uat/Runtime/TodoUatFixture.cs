using Brinell.Core.Composition;
using Brinell.Maui.Interfaces;
using Brinell.Samples.Todo.UITests;
using Brinell.Samples.Todo.UITests.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Samples.Todo.Uat.Runtime;

/// <summary>
/// The Todo app, hermetic and seeded with the <c>three-todos</c> scenario, presented to the UAT
/// engine as its root.
/// </summary>
/// <remarks>
/// The base fixture launches the app and stands up the WireMock backend; this adds the two things
/// the UAT runtime needs. <see cref="Composition"/> is where it discovers the page objects (from
/// <c>[TestModuleScan]</c>) and how it constructs them (the scope resolves <see cref="IMauiTestContext"/>).
/// Per-scenario reset is the base's <c>StartTest</c>, called from the test class's
/// <c>BeforeScenario</c>.
/// </remarks>
[TestModuleScan(typeof(TodoListPage), NamespacePrefix = "Brinell.Samples.Todo.UITests.Pages")]
public sealed class TodoUatFixture : TodoAppFixture
{
    /// <summary>Builds the composition the UAT runtime reads for page discovery and construction.</summary>
    public TodoUatFixture()
    {
        Composition = TestComposition.ForFixture(this, services =>
            services.AddSingleton<IMauiTestContext>(Context));
    }

    /// <inheritdoc />
    protected override string ScenarioName => "three-todos";

    /// <summary>The page graph and DI scope the UAT runtime resolves controls through.</summary>
    public TestComposition Composition { get; }
}
