using System.ComponentModel;
using System.Reflection;
using Brinell.Core.Interfaces;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Brinell.Uat;

/// <summary>
/// A <c>[Fact]</c> that leaves a screenshot when it fails, taken the moment the test method
/// failed, before the test object is disposed: the page as the failure left it. The screenshot
/// comes from the test object or a fixture it was given, whichever holds a test context that takes
/// its own (<see cref="IScreenshotSource"/>, as Html's Playwright context is) or a screenshot service.
/// A UAT scenario's failure already gets one (<see cref="UatScenarioTestBase{TFixture}"/>).
/// </summary>
[XunitTestCaseDiscoverer("Brinell.Uat.UiFactDiscoverer", "Brinell.Uat")]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UiFactAttribute : FactAttribute
{
}

/// <summary>Finds a <see cref="UiFactAttribute"/>'s case.</summary>
/// <param name="diagnosticMessageSink">Where discovery reports problems.</param>
// xUnit v2 only, as UatScenarioTheoryDiscoverer: v3 gives a test its outcome directly.
public sealed class UiFactDiscoverer(IMessageSink diagnosticMessageSink) : FactDiscoverer(diagnosticMessageSink)
{
    /// <inheritdoc />
    protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        => new UiTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
}

/// <summary>A <see cref="UiFactAttribute"/>'s case: an <see cref="XunitTestCase"/> run by <see cref="UiTestCaseRunner"/>.</summary>
public sealed class UiTestCase : XunitTestCase
{
    /// <summary>For the deserializer.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public UiTestCase()
    {
    }

    /// <summary>Creates the case.</summary>
    /// <param name="diagnosticMessageSink">Where problems are reported.</param>
    /// <param name="defaultMethodDisplay">How the method is shown.</param>
    /// <param name="defaultMethodDisplayOptions">How the method name is shown.</param>
    /// <param name="testMethod">The test.</param>
    public UiTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
    {
    }

    /// <inheritdoc />
    public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus,
        object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        => new UiTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus,
            aggregator, cancellationTokenSource).RunAsync();
}

/// <summary>Runs a <see cref="UiTestCase"/> with <see cref="UiTestRunner"/>.</summary>
public sealed class UiTestCaseRunner(
    IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments,
    object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator,
    CancellationTokenSource cancellationTokenSource)
    : XunitTestCaseRunner(testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus,
        aggregator, cancellationTokenSource)
{
    /// <inheritdoc />
    protected override XunitTestRunner CreateTestRunner(ITest test, IMessageBus messageBus, Type testClass,
        object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason,
        IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
        => new UiTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments,
            skipReason, beforeAfterAttributes, new ExceptionAggregator(aggregator), cancellationTokenSource);
}

/// <summary>Runs one test through <see cref="UiTestInvoker"/>.</summary>
public sealed class UiTestRunner(
    ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
    object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
    ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    : XunitTestRunner(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason,
        beforeAfterAttributes, aggregator, cancellationTokenSource)
{
    /// <inheritdoc />
    protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        => new UiTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments,
            BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync();
}

/// <summary>Invokes the test method, and takes the screenshot when it failed, before the test object is disposed.</summary>
public sealed class UiTestInvoker(
    ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod,
    object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
    ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    : XunitTestInvoker(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments,
        beforeAfterAttributes, aggregator, cancellationTokenSource)
{
    /// <inheritdoc />
    protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
    {
        var time = await base.InvokeTestMethodAsync(testClassInstance).ConfigureAwait(false);
        if (Aggregator.HasExceptions)
        {
            UiFailureScreenshots.Capture(testClassInstance, ConstructorArguments, TestClass.Name, TestMethod.Name, Aggregator.ToException());
        }

        return time;
    }
}

/// <summary>Where a failed test's screenshot comes from, and taking it.</summary>
public static class UiFailureScreenshots
{
    /// <summary>
    /// Takes the screenshot of a failed test: from the test object's own context (a property,
    /// public or not, whose value takes screenshots), else from a fixture it was given (as
    /// <see cref="UatScenarioResults.ScreenshotServiceOf"/> finds one). Never fails the test.
    /// </summary>
    /// <returns>Where it was saved; null when nothing could take one.</returns>
    public static string? Capture(object? testObject, IEnumerable<object> fixtures, string testClass, string testMethod, Exception? failure)
    {
        var service = (testObject is null ? null : ServiceOnProperties(testObject))
            ?? fixtures.Where(fixture => fixture is not null).Select(UatScenarioResults.ScreenshotServiceOf).FirstOrDefault(found => found is not null);
        if (service is null)
        {
            return null;
        }

        try
        {
            var path = failure is null
                ? service.Capture(testClass, testMethod, "failure")
                : service.CaptureOnFailure(testClass, testMethod, failure);
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            // A screenshot is evidence for a failure already reported; it never adds one.
            System.Diagnostics.Debug.WriteLine($"No failure screenshot: {error.Message}");
            return null;
        }
    }

    private static IScreenshotService? ServiceOnProperties(object holder)
    {
        if (holder is IScreenshotSource source)
        {
            return source.Screenshots;
        }

        for (var type = holder.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                switch (ValueOrNull(property, holder))
                {
                    case IScreenshotSource found:
                        return found.Screenshots;
                    case IScreenshotService service:
                        return service;
                }
            }
        }

        return null;
    }

    private static object? ValueOrNull(PropertyInfo property, object owner)
    {
        try
        {
            return property.GetValue(owner);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }
}
