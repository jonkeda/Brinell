namespace Brinell.NativeAndroid.UITests.Tests;

/// <summary>An alert dialog, the system's permission prompt, a snackbar, and a busy page.</summary>
[Collection(GalleryCollection.Name)]
public sealed class DialogsTests(GalleryFixture gallery)
{
    private DialogsPage Open()
    {
        gallery.Open("dialogs");
        var page = new DialogsPage(gallery.Context);
        page.AssertIdle();
        return page;
    }

    [Fact]
    public void AnAlertDialog_ShowsItsTitleAndMessage_AndItsButtonsAnswer()
    {
        var page = Open();

        page.ShowAlert.Click();
        page.Alert.Title.AssertText("Delete everything?");
        page.Alert.Message.AssertText("This cannot be undone.");
        page.Alert.NegativeButton.Click();

        page.Status.AssertText("Kept");
        page.Alert.AssertExists(false);
    }

    /// <summary>
    /// While the system's permission prompt covers the app, the app is not ready; the prompt is,
    /// and answering it gives the app back. The session installs the app fresh, so the prompt
    /// always comes; the tap waits for it.
    /// </summary>
    [Fact]
    public void ThePermissionPrompt_IsAnswered_AndTheAppCarriesOn()
    {
        var page = Open();

        page.AskCamera.Click();
        page.Permission.Allow.Click();

        page.Status.AssertText("Camera allowed");
    }

    [Fact]
    public void ASnackbar_ShowsItsText_AndItsActionAnswers()
    {
        var page = Open();

        page.ShowSnackbar.Click();
        page.Snackbar.Text.AssertText("Archived");
        page.Snackbar.Action.Click();

        page.Status.AssertText("Unarchived");
    }

    /// <summary>A page that is busy for two seconds: the next call waits it out, then reads the result.</summary>
    [Fact]
    public void ABusyPage_IsWaitedOut()
    {
        var page = Open();

        page.Work.Click();
        page.WaitBusy(true, timeoutMs: 2000);

        page.Status.AssertText("Work done");
        Assert.False(page.IsBusy());
    }
}
