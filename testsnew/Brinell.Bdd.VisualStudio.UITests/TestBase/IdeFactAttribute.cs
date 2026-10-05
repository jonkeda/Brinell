namespace Brinell.Bdd.VisualStudio.UITests.TestBase;

/// <summary>
/// A test that starts Visual Studio: skipped unless <c>BRINELL_IDE_TESTS=1</c>, so a run with nothing
/// set stays green. It takes the desktop for a minute or more; run one UI test process at a time.
/// </summary>
public sealed class IdeFactAttribute : FactAttribute
{
    /// <summary>The variable that turns these tests on.</summary>
    public const string Variable = "BRINELL_IDE_TESTS";

    public IdeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(Variable) != "1")
        {
            Skip = $"Starts Visual Studio: set {Variable}=1 to run it (a desktop, and Visual Studio).";
        }
    }
}
