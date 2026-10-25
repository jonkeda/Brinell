using System.Reflection;
using Bruinell.Core.Testing;
using Xunit.Sdk;

namespace Bruinell.Uat;

/// <summary>
/// Names every test of the assembly in its <see cref="TestScope"/>, as <c>Namespace.Class.Method</c>, so a plain
/// <c>[Fact]</c>'s calls name their test in the timings and the touch map as <see cref="UiFactAttribute"/>'s and the
/// scenarios' do (.my/test-selection/next-plan.md, item 3). Put it on the assembly: <c>[assembly: TestScope]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class TestScopeAttribute : BeforeAfterTestAttribute
{
    private static readonly AsyncLocal<IDisposable?> Entered = new();

    /// <summary>The name a test has in its scope: its class's full name and its method's.</summary>
    public static string NameOf(MethodInfo method) => $"{(method.ReflectedType ?? method.DeclaringType)?.FullName}.{method.Name}";

    /// <inheritdoc />
    /// <remarks>
    /// Also a <c>test</c> span in the timings, the test's window on their clock: what another process logged, such as SQL
    /// Server's Extended Events, is joined to the test by time (<c>bruinell sql report</c>).
    /// </remarks>
    public override void Before(MethodInfo methodUnderTest)
    {
        var name = NameOf(methodUnderTest);
        var scope = TestScope.EnterTest(name);
        Entered.Value = new Entry(scope, Bruinell.Core.Logging.Timing.Span("test", name));
    }

    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest)
    {
        Entered.Value?.Dispose();
        Entered.Value = null;
    }

    // The span ends inside the test's scope, then the scope.
    private sealed class Entry(IDisposable scope, IDisposable span) : IDisposable
    {
        public void Dispose()
        {
            span.Dispose();
            scope.Dispose();
        }
    }
}
