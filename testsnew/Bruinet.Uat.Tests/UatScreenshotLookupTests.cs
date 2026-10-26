using Bruinet.Core.Interfaces;
using Xunit;

namespace Bruinet.Uat.Tests;

/// <summary>
/// Where a failing scenario's screenshot comes from: the fixture's own service, or a test
/// context on it that takes its own (Html's Playwright context).
/// </summary>
public sealed class UatScreenshotLookupTests
{
    private static readonly IScreenshotService Service = new Stub();

    private sealed class Stub : IScreenshotService
    {
        public Bruinet.Core.Configuration.ScreenshotSettings Settings { get; } = new();

        public string Capture(string? description = null) => string.Empty;

        public string Capture(string testClass, string testMethod, string description) => string.Empty;

        public string CaptureOnFailure(string testClass, string testMethod, Exception exception) => string.Empty;
    }

    private sealed class Source : IScreenshotSource
    {
        public IScreenshotService Screenshots => Service;
    }

    private sealed class FixtureWithAContext
    {
        public object Context { get; } = new Source();
    }

    private sealed class FixtureNotStarted
    {
        public object Context => throw new InvalidOperationException("The fixture is not started.");

        public object Other { get; } = new Source();
    }

    private sealed class FixtureWithout
    {
        public string Name { get; } = "plain";
    }

    [Fact]
    public void AContextOnAPublicProperty_GivesItsScreenshots()
        => Assert.Same(Service, UatScenarioResults.ScreenshotServiceOf(new FixtureWithAContext()));

    [Fact]
    public void APropertyThatThrows_IsPassedOver()
        => Assert.Same(Service, UatScenarioResults.ScreenshotServiceOf(new FixtureNotStarted()));

    [Fact]
    public void AFixtureThatIsASource_GivesItsScreenshots()
        => Assert.Same(Service, UatScenarioResults.ScreenshotServiceOf(new Source()));

    [Fact]
    public void AFixtureWithNone_GivesNone()
        => Assert.Null(UatScenarioResults.ScreenshotServiceOf(new FixtureWithout()));
}
