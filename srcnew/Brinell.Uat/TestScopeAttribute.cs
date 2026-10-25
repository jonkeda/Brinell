using System.Reflection;
using Brinell.Core.Testing;
using Xunit.Sdk;

namespace Brinell.Uat;

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
    public override void Before(MethodInfo methodUnderTest) => Entered.Value = TestScope.EnterTest(NameOf(methodUnderTest));

    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest)
    {
        Entered.Value?.Dispose();
        Entered.Value = null;
    }
}
