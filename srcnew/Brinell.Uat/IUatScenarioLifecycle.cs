namespace Brinell.Uat;

/// <summary>
/// What a fixture does before a file's scenarios and before each scenario: its test set, a clean
/// start. <see cref="UatScenarioTestBase{TFixture}"/> calls it, and so does a host that runs
/// scenarios without the suite's test class (Presenter), so both start a scenario the same way.
/// </summary>
public interface IUatScenarioLifecycle
{
    /// <summary>Before the first scenario of a file: load what its metadata asks for, such as its <c>TestData</c>.</summary>
    /// <param name="document">The file, as parsed.</param>
    void BeforeFile(BddDocument document);

    /// <summary>Before each scenario: a clean start.</summary>
    /// <param name="scenario">The scenario about to run.</param>
    void BeforeScenario(BddBoundScenario scenario);
}
