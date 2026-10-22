using Brinell.Core.Exceptions;
using Brinell.Maui.Testing;

namespace Brinell.Maui.Tests;

/// <summary>
/// A fixture replaces a session when its driver says the app is gone, by the error's type; the
/// platform's messages are read once, in the driver (AppiumErrors.Live).
/// </summary>
public class SessionLostTests
{
    [Fact]
    public void AppUnavailable_IsALostSession()
        => Assert.True(MauiTestFixtureBase.IsSessionLost(new AppUnavailableException("the Appium session has ended.")));

    [Fact]
    public void AppUnavailable_InsideAnotherError_IsALostSession()
        => Assert.True(MauiTestFixtureBase.IsSessionLost(
            new InvalidOperationException("A step failed.", new AppUnavailableException("the Appium session has ended."))));

    [Fact]
    public void AnErrorThatOnlySaysSo_IsNot()
        => Assert.False(MauiTestFixtureBase.IsSessionLost(new InvalidOperationException("invalid session id")));
}
