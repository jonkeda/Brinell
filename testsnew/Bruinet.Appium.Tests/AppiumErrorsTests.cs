using Bruinet.Appium;
using OpenQA.Selenium;

namespace Bruinet.Appium.Tests;

/// <summary>
/// How the Appium driver recognizes a replaced element and an ended session.
/// </summary>
/// <remarks>
/// Pins the classification behind <c>StaleElementException</c> and <c>AppUnavailableException</c>
/// on Android and iOS (stale-readiness plan, step 3). An ended session is recognized only by the
/// fixed W3C wording, never by a broad word, so an ordinary error is not reported as a lost app.
/// </remarks>
public class AppiumErrorsTests
{
    [Fact]
    public void StaleElementReference_IsElementGone()
        => Assert.True(AppiumErrors.IsElementGone(new StaleElementReferenceException("stale element reference")));

    [Theory]
    [InlineData("invalid session id")]
    [InlineData("A session is either terminated or not started")]
    public void TheW3CSessionErrors_AreSessionGone(string message)
        => Assert.True(AppiumErrors.IsSessionGone(new WebDriverException(message)));

    [Theory]
    [InlineData("An element could not be located on the page using the given search parameters.")]
    [InlineData("The connection was closed")]
    [InlineData("Resource not found")]
    public void OrdinaryErrors_AreNotSessionGone(string message)
        => Assert.False(AppiumErrors.IsSessionGone(new WebDriverException(message)));

    [Fact]
    public void AnUnrelatedException_IsNeither()
    {
        var error = new InvalidOperationException("not supported");

        Assert.False(AppiumErrors.IsElementGone(error));
        Assert.False(AppiumErrors.IsSessionGone(error));
        Assert.False(AppiumErrors.IsUnknownCommand(error));
    }

    [Theory]
    [InlineData("Unknown mobile command \"scrollGesture\". Only shell, startActivity commands are supported.")]
    [InlineData("unknown command: The requested resource could not be found")]
    [InlineData("Method is not implemented")]
    public void ACommandTheServerLacks_IsUnknownCommand(string message)
        => Assert.True(AppiumErrors.IsUnknownCommand(message.StartsWith("Method", StringComparison.Ordinal) ? new UnknownMethodException(message) : new WebDriverException(message)));

    [Fact]
    public void ASessionError_IsNotUnknownCommand()
        => Assert.False(AppiumErrors.IsUnknownCommand(new WebDriverException("invalid session id")));

    [Theory]
    [InlineData("invalid session id")]
    [InlineData("A session is either terminated or not started")]
    [InlineData("'POST /elements' cannot be proxied to UiAutomator2 server because the instrumentation process is not running (probably crashed)")]
    public void Live_ReportsAnEndedSession_AsAppUnavailable(string message)
        => Assert.IsType<Bruinet.Core.Exceptions.AppUnavailableException>(
            Record.Exception(() => AppiumErrors.Live(() => throw new WebDriverException(message))));

    [Fact]
    public void Live_ReportsAStuckServer_AsAppUnavailable()
        => Assert.IsType<Bruinet.Core.Exceptions.AppUnavailableException>(Record.Exception(() => AppiumErrors.Live(
            () => throw new WebDriverException("The HTTP request to the remote WebDriver server for URL http://127.0.0.1:4723/session/x/elements timed out after 60 seconds."))));

    [Fact]
    public void Live_ReportsAReplacedElement_AsStale()
        => Assert.IsType<Bruinet.Core.Exceptions.StaleElementException>(
            Record.Exception(() => AppiumErrors.Live(() => throw new StaleElementReferenceException("stale element reference"))));

    [Fact]
    public void Live_LetsAnOrdinaryErrorThrough_AsItIs()
    {
        var error = new WebDriverException("An unknown server-side error occurred while processing the command.");

        Assert.Same(error, Record.Exception(() => AppiumErrors.Live(() => throw error)));
    }
}
