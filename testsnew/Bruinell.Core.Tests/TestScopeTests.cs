using Bruinell.Core.Logging;
using Bruinell.Core.Testing;

namespace Bruinell.Core.Tests;

public sealed class TestScopeTests
{
    [Fact]
    public void Enter_TestScenarioStep_NestAndPutBack()
    {
        using (TestScope.EnterTest("T"))
        {
            using (TestScope.EnterScenario("S"))
            {
                using (TestScope.EnterStep("3: I tap Save"))
                {
                    Assert.Equal(new TestScope("T", "S", "3: I tap Save"), TestScope.Current);
                }

                Assert.Equal(new TestScope("T", "S", null), TestScope.Current);
            }

            Assert.Equal(new TestScope("T", null, null), TestScope.Current);
        }

        Assert.Equal(TestScope.Empty, TestScope.Current);
    }

    [Fact]
    public async Task Enter_FlowsIntoAwaitedCode()
    {
        using var test = TestScope.EnterTest("T");

        var seen = await Task.Run(() => TestScope.Current.Test);

        Assert.Equal("T", seen);
    }

    [Fact]
    public void EnterTest_StartsWithoutTheScenarioBefore()
    {
        using var outer = TestScope.EnterScenario("old");

        using var test = TestScope.EnterTest("T");

        Assert.Null(TestScope.Current.Scenario);
    }

    // These suites run without BRUINELL_TIMINGS: a span costs nothing and writes nothing.
    [Fact]
    public void Span_WithoutTheVariable_IsNothing()
    {
        Assert.False(Timing.Enabled);
        using (Timing.Span("step", "x"))
        {
        }
    }
}
