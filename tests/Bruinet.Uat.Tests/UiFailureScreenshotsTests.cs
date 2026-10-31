using Bruinet.Core.Configuration;
using Bruinet.Core.Interfaces;
using Xunit;

namespace Bruinet.Uat.Tests;

/// <summary>
/// Where a failed <see cref="UiFactAttribute"/> test's screenshot comes from: the test object's own
/// context, public or not, or a fixture it was given; nothing at all when neither has one.
/// </summary>
public sealed class UiFailureScreenshotsTests
{
    private sealed class Camera : IScreenshotService, IScreenshotSource
    {
        public List<string> Taken { get; } = [];

        public IScreenshotService Screenshots => this;

        public ScreenshotSettings Settings { get; } = new();

        public string Capture(string? description = null) => Take(description ?? "manual");

        public string Capture(string testClass, string testMethod, string description) => Take($"{testClass}.{testMethod}.{description}");

        public string CaptureOnFailure(string testClass, string testMethod, Exception exception) => Take($"{testClass}.{testMethod}.failure");

        private string Take(string name)
        {
            Taken.Add(name);
            return $"{name}.png";
        }
    }

    private sealed class TestWithAPrivateContext(Camera camera)
    {
        private Camera Context { get; } = camera;
    }

    private sealed class Fixture(Camera camera)
    {
        public object Context { get; } = camera;
    }

    [Fact]
    public void TheTestObjectsOwnContext_TakesIt_EvenWhenNotPublic()
    {
        var camera = new Camera();

        var path = UiFailureScreenshots.Capture(new TestWithAPrivateContext(camera), [], "PageTests", "Click_Works", new InvalidOperationException("failed"));

        Assert.Equal("PageTests.Click_Works.failure.png", path);
        Assert.Single(camera.Taken);
    }

    [Fact]
    public void AFixturesContext_TakesIt_WhenTheTestObjectHasNone()
    {
        var camera = new Camera();

        var path = UiFailureScreenshots.Capture(new object(), [new Fixture(camera)], "PageTests", "Click_Works", null);

        Assert.Equal("PageTests.Click_Works.failure.png", path);
    }

    [Fact]
    public void WithNoContextAnywhere_NothingIsTaken()
        => Assert.Null(UiFailureScreenshots.Capture(new object(), [new object()], "PageTests", "Click_Works", null));
}
