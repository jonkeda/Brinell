using Brinell.Uat;

namespace Brinell.Samples.Todo.Uat.Runtime;

/// <summary>
/// Runs every <c>Scenarios/*.uat.md</c> against the hermetic Todo app.
/// </summary>
/// <remarks>
/// Each scenario gets a clean start through <see cref="BeforeScenario"/>, which calls the fixture's
/// <c>StartTest</c>: online again, backend reset to <c>three-todos</c>, the list on screen. Scenarios
/// therefore stay independent of order and of each other's edits.
/// </remarks>
[Collection(TodoUatCollection.CollectionName)]
[Trait("Category", "UAT")]
[Trait("Target", "MAUI")]
public sealed class TodoUatScenarioTests : UatScenarioTestBase<TodoUatFixture>
{
    /// <summary>Creates the scenario runner over the shared fixture.</summary>
    /// <param name="fixture">The hermetic Todo app.</param>
    public TodoUatScenarioTests(TodoUatFixture fixture)
        : base(fixture)
    {
    }

    /// <summary>The <c>Scenarios</c> folder's UAT files, one theory case each.</summary>
    public static IEnumerable<object[]> ScenarioFiles => GetScenarioFiles();

    /// <summary>Runs one UAT file and asserts it passes.</summary>
    /// <param name="filePath">The scenario file.</param>
    [Theory(Timeout = 120000)]
    [MemberData(nameof(ScenarioFiles))]
    public Task UatFile_Passes(string filePath) => RunUatFileAsync(filePath);

    /// <inheritdoc />
    protected override void BeforeScenario(UatBoundScenario scenario) => Fixture.StartTest();

    /// <inheritdoc />
    protected override UatRuntimeValidationOptions RuntimeValidation { get; } =
        new(Target: "MAUI", Fixture: "TodoUatFixture");
}
