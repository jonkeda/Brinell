using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Tests.Scroll;

/// <summary>
/// Tests scrolling on its own terms.
/// </summary>
/// <remarks>
/// <para>
/// Scrolling used to be exercised only incidentally, by tests whose Reset button happened to sit
/// below the fold, so a scroll defect surfaced as an unrelated assertion about a status label.
/// These tests make the scroll itself the subject.
/// </para>
/// <para>
/// The page is deliberately taller than any screen, with the status label first and the buttons
/// spread down it, so acting on one and reading the result crosses the full height in both
/// directions.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Control", "ScrollView")]
public class ScrollTests
{
    private readonly MauiFixture _fixture;

    public ScrollTests(MauiFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>1. A control above the fold works with no scrolling involved — the control case.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task TopButton_Click_UpdatesStatus()
    {
        var page = _fixture.NavigateToScroll();

        page.TopButton.Click();

        page.StatusLabel.AssertTextContains("top pressed");
        return Task.CompletedTask;
    }

    /// <summary>2. Clicking a button that is below the fold requires scrolling down to it.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task BottomButton_Click_UpdatesStatus()
    {
        var page = _fixture.NavigateToScroll();

        page.BottomButton.Click();

        // The label is at the top of the page, so this read also has to come back up.
        page.StatusLabel.AssertTextContains("bottom pressed");
        return Task.CompletedTask;
    }

    /// <summary>3. The same, one screen down rather than at the very end.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task MiddleButton_Click_UpdatesStatus()
    {
        var page = _fixture.NavigateToScroll();

        page.MiddleButton.Click();

        page.StatusLabel.AssertTextContains("middle pressed");
        return Task.CompletedTask;
    }

    /// <summary>4. Scrolling down and then back up again — the direction the Reset tests failed on.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "Click")]
    public Task TopButton_Click_AfterScrollingToTheBottom_UpdatesStatus()
    {
        var page = _fixture.NavigateToScroll();

        page.BottomButton.Click();
        page.StatusLabel.AssertTextContains("bottom pressed");

        page.TopButton.Click();

        page.StatusLabel.AssertTextContains("top pressed");
        return Task.CompletedTask;
    }

    /// <summary>5. Reading text from a control below the fold.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "GetText")]
    public Task BottomLabel_Text_IsReadable()
    {
        var page = _fixture.NavigateToScroll();

        page.BottomLabel.AssertTextContains("bottom reached");
        return Task.CompletedTask;
    }

    /// <summary>
    /// 6. The two visibility questions, on one control, in one place.
    /// </summary>
    /// <remarks>
    /// <c>IsVisible</c> asks whether the control is on screen now and must answer false; the
    /// bottom label cannot be on screen while the page is scrolled to the top.
    /// <c>IsVisibleAfterScroll</c> asks whether it can be seen at all and must answer true.
    /// Both answers have to be the same on Windows and Android, which is the whole reason the
    /// two are separate methods.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsVisible")]
    public Task BottomLabel_IsVisible_IsFalseUntilScrolledTo()
    {
        var page = _fixture.NavigateToScroll();

        Assert.False(page.BottomLabel.IsVisible());
        Assert.True(page.BottomLabel.IsVisibleAfterScroll());
        return Task.CompletedTask;
    }

    /// <summary>7. A control below the fold still exists, whether or not it is on screen.</summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    [Trait("Method", "IsExists")]
    public Task BottomButton_Exists_WithoutScrolling()
    {
        var page = _fixture.NavigateToScroll();

        page.BottomButton.AssertExists();
        return Task.CompletedTask;
    }
}
