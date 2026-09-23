using Brinell.Core.Utilities;
using Brinell.Maui.Exceptions;
using Brinell.Maui.UITests.Pages;
using Xunit;

namespace Brinell.Maui.UITests.Tests.Navigation;

/// <summary>
/// Raising a toolbar item without the pointer.
/// </summary>
/// <remarks>
/// <para>
/// <b>A toolbar item cannot be driven through UI Automation on Windows.</b> Its automation peer
/// accepts the Invoke pattern, reports success and raises nothing - measured four ways, see
/// <c>ToolbarButton</c>. Until this verb, the only route was a real mouse click, which is why the
/// two <c>BackToHub</c> tests in <c>AppRootScopeTests</c> used to skip themselves.
/// </para>
/// <para>
/// <b>The items used here do not navigate.</b> The sample's only other toolbar item is "Back", and
/// a verb exercised only against navigation cannot tell "the item was raised" from "the page
/// happened to change".
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class ToolbarVerbTests
{
    private readonly MauiFixture _fixture;

    public ToolbarVerbTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToNavigationDemo();
    }

    private NavigationDemoPage Page => _fixture.NavigationDemoPage;

    /// <summary>
    /// A toolbar item runs, without the pointer.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task InvokeToolbarItem_RaisesTheItem_WithoutThePointer()
    {
        _fixture.Context.AppElement.InvokeToolbarItem("PageToolbarRefresh");

        Page.LastAction.AssertText("PageToolbar/Refresh");

        return Task.CompletedTask;
    }

    /// <summary>
    /// A disabled item is refused, and nothing runs.
    /// </summary>
    /// <remarks>
    /// The item's command would record if it ran, so "none" afterwards proves the refusal came
    /// before the command rather than after it.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task InvokeToolbarItem_WhenTheItemIsDisabled_IsRefusedAndDoesNothing()
    {
        var refusal = Assert.Throws<ElementNotReadyException>(
            () => _fixture.Context.AppElement.InvokeToolbarItem("PageToolbarDelete"));

        Assert.Equal(NotReadyReason.Disabled, refusal.Reason);
        Assert.Contains("disabled", refusal.Message);
        Page.LastAction.AssertText("none");

        return Task.CompletedTask;
    }

    /// <summary>
    /// An unknown id is refused with a reason, rather than quietly doing nothing.
    /// </summary>
    /// <remarks>
    /// A menu item's id is not a toolbar item's, even on the same page: the two verbs search
    /// different collections, which is the point of having two.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task InvokeToolbarItem_WithNoSuchItem_IsRefusedWithTheReason()
    {
        var refusal = Assert.Throws<BrinellException>(
            () => _fixture.Context.AppElement.InvokeToolbarItem("PageMenuFileNew"));

        Assert.Contains("carries that AutomationId", refusal.Message);
        Page.LastAction.AssertText("none");

        return Task.CompletedTask;
    }
}
