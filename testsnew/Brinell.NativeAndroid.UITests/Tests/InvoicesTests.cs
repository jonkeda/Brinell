namespace Brinell.NativeAndroid.UITests.Tests;

/// <summary>A recycler view of 200 rows: rows keyed by their item, found by scrolling, and followed when recycled.</summary>
[Collection(GalleryCollection.Name)]
public sealed class InvoicesTests(GalleryFixture gallery)
{
    private InvoicesPage Open()
    {
        gallery.Open("invoices");
        var page = new InvoicesPage(gallery.Context);
        page.AssertIdle();
        page.Count.AssertText("200 invoices");
        return page;
    }

    [Fact]
    public void ARow_IsKeyedByItsName_AndReadByItsViews()
    {
        var page = Open();

        var row = page.Invoices.Item("Invoice 3");

        Assert.Equal("Invoice 3", row.Key.Value);
        row.Text("amount").AssertText("€ 30,00");
    }

    /// <summary>A row far below the fold is found by scrolling the list, within the call's budget.</summary>
    [Fact]
    public void ARowFarDown_IsFoundByScrolling()
    {
        var page = Open();

        var row = page.Invoices.Item("Invoice 42", timeoutMs: 30000);

        row.Text("amount").AssertText("€ 420,00");
    }

    /// <summary>
    /// Deleting a row rebinds the views below it to other items. The row object of the item after
    /// the deleted one follows its item; it never answers for the item now in its old place.
    /// </summary>
    [Fact]
    public void AfterADelete_ARowFollowsItsItem_NotItsOldPlace()
    {
        var page = Open();
        var fourth = page.Invoices.Item("Invoice 4");
        var oldPosition = fourth.Position;

        page.Invoices.Item("Invoice 2").Button("delete").Click();

        // A snackbar leaves by itself: read it first.
        page.Snackbar.Text.AssertText("Deleted Invoice 2");
        page.Count.AssertText("199 invoices");
        fourth.Text("amount").AssertText("€ 40,00");
        Assert.Equal("Invoice 5", page.Invoices.Item(oldPosition).Key.Value);
    }

    [Fact]
    public void Undo_OnTheSnackbar_PutsTheRowBack()
    {
        var page = Open();

        page.Invoices.Item("Invoice 1").Button("delete").Click();
        page.Snackbar.Action.Click();

        page.Count.AssertText("200 invoices");
        page.Invoices.Item("Invoice 1").Text("amount").AssertText("€ 10,00");
    }

    /// <summary>Pulling to refresh makes the page busy; the next call waits it out.</summary>
    [Fact]
    public void PullToRefresh_IsWaitedOut_ThroughTheBusySignal()
    {
        var page = Open();

        page.Refresh.PullToRefresh();

        page.Count.AssertText("200 invoices, refreshed");
    }
}
