using Bruinet.Uat;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Bruinet.Samples.Todo.Uat.Runtime;

/// <summary>
/// Runs scenario cases in <see cref="BddScenarioSource.GetOrderKey"/> order - by test set, then
/// file, then position - instead of xUnit's default order by case id, which interleaves files and
/// would restart the app for nearly every scenario.
/// </summary>
public sealed class BddScenarioOrderer : ITestCaseOrderer
{
    /// <summary>For <see cref="TestCaseOrdererAttribute"/>.</summary>
    public const string TypeName = "Bruinet.Samples.Todo.Uat.Runtime.BddScenarioOrderer";

    /// <summary>For <see cref="TestCaseOrdererAttribute"/>.</summary>
    public const string AssemblyName = "Bruinet.Samples.Todo.Uat";

    /// <inheritdoc />
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
        => testCases.OrderBy(
            testCase => testCase.TestMethodArguments is [string file, string scenario, ..]
                ? BddScenarioSource.GetOrderKey(file, scenario)
                : testCase.DisplayName,
            StringComparer.Ordinal);
}
