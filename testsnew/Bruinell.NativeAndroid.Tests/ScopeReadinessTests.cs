using Bruinell.Core.Scopes;
using Bruinell.NativeAndroid.Controls.Dialogs;

namespace Bruinell.NativeAndroid.Tests;

/// <summary>Scope readiness on native Android (plan step 7): the app in front, the page, the busy view, dialogs.</summary>
public class ScopeReadinessTests
{
    private readonly FakeApp _app = new();

    private void GivenBusy(Func<bool> shown)
        => _app.Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value.Contains(PageObjectBase<TestPage>.BusyViewName))))
            .Returns(() => shown() ? FakeApp.View().Object : null);

    /// <summary>A covered app is not ready, and the failure says what covers it.</summary>
    [Fact]
    public void ACoveredApp_FailsTheCall_NamingWhatCoversIt()
    {
        _app.InFront = false;
        _app.Given("Target", () => FakeApp.View().Object);

        var error = Record.Exception(() => _app.Page().Target.Click(timeoutMs: 300));

        Assert.IsType<ScopeNotReadyException>(error);
        Assert.Contains("system dialog", error!.Message);
    }

    /// <summary>A busy page delays a control's call; the call acts once the app is idle.</summary>
    [Fact]
    public void ABusyPage_DelaysTheCall_UntilTheAppIsIdle()
    {
        var probes = 0;
        GivenBusy(() => ++probes < 4);
        var button = FakeApp.View();
        _app.Given("Target", () => button.Object);

        _app.Page().Target.Click();

        button.Verify(e => e.Click(), Times.Once);
        Assert.True(probes >= 4);
    }

    /// <summary>A page that stays busy fails the call as busy, and is never tapped.</summary>
    [Fact]
    public void APageThatStaysBusy_FailsTheCallAsBusy()
    {
        GivenBusy(() => true);
        var button = FakeApp.View();
        _app.Given("Target", () => button.Object);

        var error = Record.Exception(() => _app.Page().Target.Click(timeoutMs: 300));

        var notReady = Assert.IsType<ScopeNotReadyException>(error);
        Assert.Equal(ScopeReadinessState.Busy, notReady.Readiness.State);
        button.Verify(e => e.Click(), Times.Never);
    }

    /// <summary>A page whose activity is not in front is not loaded.</summary>
    [Fact]
    public void APageWithAnActivity_IsLoadedOnlyInThatActivity()
    {
        _app.Driver.Setup(d => d.CurrentActivity).Returns(".MainActivity");
        var page = new LoginPage(_app.Context.Object);

        Assert.False(page.IsLoaded());

        _app.Driver.Setup(d => d.CurrentActivity).Returns("com.bruinell.gallery.LoginActivity");
        Assert.True(page.IsLoaded());
    }

    /// <summary>The permission prompt is usable while it covers the app: it does not ask the app.</summary>
    [Fact]
    public void ThePermissionDialog_IsReady_WhileItCoversTheApp()
    {
        _app.InFront = false;
        var dialog = FakeApp.View();
        _app.Context.Setup(c => c.TryFindElement(It.Is<Locator>(l => l.Value.Contains("grant_dialog")))).Returns(dialog.Object);

        var prompt = new PermissionDialog<AppRoot>(new AppRoot(_app.Context.Object));

        Assert.True(prompt.IsReady());
    }

    private sealed class LoginPage(INativeAndroidTestContext context) : PageObjectBase<LoginPage>(context)
    {
        public override string? Activity => ".LoginActivity";
    }
}
