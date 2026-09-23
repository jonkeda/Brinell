using Brinell.Maui.UITests.Pages;
using Xunit;

namespace Brinell.Maui.UITests.Tests.Dialogs;

/// <summary>
/// Step 25: asserting what the app asked, not only that something was dismissed.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every dialog test in this suite could pass against the wrong question.</b> They clicked a
/// button by name and checked a label afterwards, so an app that asked "Delete everything?"
/// where it meant "Proceed?" would have gone on passing - and a confirmation is the one place
/// where asking the wrong thing and getting the right answer has consequences.
/// </para>
/// <para>
/// <b>Two sources, split by who actually knows.</b> The title and the buttons are read off the
/// platform: WinUI names the dialog after its title and each button is an element carrying its
/// own text. The message is not published that way - it sits in the dialog's content area beside
/// a second copy of the title, so from outside it can only be identified as "the text that is
/// not the title", which is silently wrong for an alert whose message and title read alike. That
/// half comes from the app, which passed the string in the first place.
/// </para>
/// </remarks>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GestureBridge")]
[Trait("Stage", "Background")]
public class AlertReadTests
{
    private readonly MauiFixture _fixture;
    private readonly DialogsTestPage _page;

    public AlertReadTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _page = fixture.NavigateToDialogs();
    }

    /// <summary>
    /// A confirmation reports its own four strings while it is on screen.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task CurrentAlert_ReportsWhatTheAppAsked()
    {
        var dialog = _page.Dialog;

        _page.ShowConfirmButton.Click();
        Assert.True(dialog.WaitExists(true, TestConstants.DefaultTestTimeoutMs));

        var asked = _fixture.Context.AppElement.ReadAlert();

        Assert.NotNull(asked);
        Assert.Equal("Confirm", asked!.Value.Title);
        Assert.Equal("Proceed?", asked.Value.Message);
        Assert.Equal("Yes", asked.Value.Accept);
        Assert.Equal("No", asked.Value.Cancel);

        dialog.DialogButton("No").Click();
        Assert.True(dialog.WaitExists(false, TestConstants.DefaultTestTimeoutMs));

        return Task.CompletedTask;
    }

    /// <summary>
    /// A one-button alert reports one button, and does not invent an affirmative.
    /// </summary>
    /// <remarks>
    /// MAUI's single-button overload passes its button as the dismissing one, and WinUI renders
    /// it as the dialog's secondary button - so this is what the app asked, reported in the terms
    /// it asked it. Promoting it to <c>Accept</c> would read better and be false.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task CurrentAlert_WithOneButton_ReportsNoAccept()
    {
        var dialog = _page.Dialog;

        _page.ShowAlertButton.Click();
        Assert.True(dialog.WaitExists(true, TestConstants.DefaultTestTimeoutMs));

        var asked = _fixture.Context.AppElement.ReadAlert();

        Assert.NotNull(asked);
        Assert.Equal("Alert", asked!.Value.Title);
        Assert.Equal("This is an alert.", asked.Value.Message);
        Assert.Equal(string.Empty, asked.Value.Accept);
        Assert.Equal(["OK"], asked.Value.Buttons);

        dialog.DialogButton("OK").Click();
        Assert.True(dialog.WaitExists(false, TestConstants.DefaultTestTimeoutMs));

        return Task.CompletedTask;
    }

    /// <summary>
    /// With nothing open, the app says so rather than refusing.
    /// </summary>
    /// <remarks>
    /// <b>The difference between an answer and a refusal, again.</b> "No alert is open" is a fact
    /// about the app and the commonest state it is in; if the verb refused instead, a caller
    /// could not tell it apart from an app with no bridge, and the natural response to that - wait
    /// and ask again - would be waiting for something that has already been answered.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task CurrentAlert_WithNothingOpen_IsNull()
    {
        Assert.Null(_fixture.Context.AppElement.ReadAlert());

        return Task.CompletedTask;
    }

    /// <summary>
    /// The question stops being readable when the dialog goes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Asked the instant the dialog goes, with nothing in between, because that is the window
    /// this got wrong.</b> The app clears its record when <c>DisplayAlert</c>'s await resumes -
    /// a continuation queued on the UI thread, and therefore some moments after the dialog has
    /// already left the tree. A test that dismissed a prompt and immediately asked was told
    /// about the prompt it had just closed, which is the worst kind of stale answer: plausible,
    /// and about the right dialog.
    /// </para>
    /// <para>
    /// The fix was to let each end answer only what it can see - the screen says whether a
    /// dialog is up, the app says what it asks - so any assertion in between here would hide
    /// the race rather than test it.
    /// </para>
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task CurrentAlert_AfterDismissal_IsNullAgain()
    {
        var dialog = _page.Dialog;

        _page.ShowConfirmButton.Click();
        Assert.True(dialog.WaitExists(true, TestConstants.DefaultTestTimeoutMs));
        Assert.NotNull(_fixture.Context.AppElement.ReadAlert());

        dialog.DialogButton("Yes").Click();
        Assert.True(dialog.WaitExists(false, TestConstants.DefaultTestTimeoutMs));

        Assert.Null(_fixture.Context.AppElement.ReadAlert());

        _page.Result.AssertText("confirmed");

        return Task.CompletedTask;
    }

    /// <summary>
    /// The title and the buttons come from the platform, with no app cooperation.
    /// </summary>
    /// <remarks>
    /// <b>What an app that cannot be modified still gets.</b> Half of step 25 needs a line in the
    /// app under test; this half needs nothing, so a third-party app is not left with "something
    /// opened" as its only assertion. The button list is worth having on its own: a confirmation
    /// offering no way out is a defect no assertion about the outcome can see, because the test
    /// presses a button by name and passes either way.
    /// </remarks>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task Dialog_PublishesItsTitleAndButtons()
    {
        var dialog = _page.Dialog;

        _page.ShowConfirmButton.Click();
        Assert.True(dialog.WaitExists(true, TestConstants.DefaultTestTimeoutMs));

        dialog.AssertTitle("Confirm")
            .AssertButtonTexts(["Yes", "No"])
            .AssertButtonTextsHasItem("No")
            .AssertButtonTextsCount(2)
            // And the half that does need the app, reached from the same place a reader would look.
            .AssertMessage("Proceed?");
        Assert.Equal("Confirm", dialog.GetTitle());

        dialog.DialogButton("No").Click();
        Assert.True(dialog.WaitExists(false, TestConstants.DefaultTestTimeoutMs));

        return Task.CompletedTask;
    }

    /// <summary>
    /// A prompt is an alert too, and reports the same four strings.
    /// </summary>
    [Fact(Timeout = TestConstants.DefaultTestTimeoutMs)]
    public Task CurrentAlert_ForAPrompt_ReportsTheQuestionAndBothButtons()
    {
        var dialog = _page.Dialog;

        _page.ShowPromptButton.Click();
        Assert.True(dialog.WaitExists(true, TestConstants.DefaultTestTimeoutMs));

        var asked = _fixture.Context.AppElement.ReadAlert();

        Assert.NotNull(asked);
        Assert.Equal("Prompt", asked!.Value.Title);
        Assert.Equal("Enter a value", asked.Value.Message);
        Assert.Equal(["OK", "Cancel"], asked.Value.Buttons);

        dialog.DialogButton("Cancel").Click();
        Assert.True(dialog.WaitExists(false, TestConstants.DefaultTestTimeoutMs));

        return Task.CompletedTask;
    }
}
