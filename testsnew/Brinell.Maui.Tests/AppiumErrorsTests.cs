using Brinell.Maui.Appium;
using OpenQA.Selenium;

namespace Brinell.Maui.Tests;

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
    }
}
