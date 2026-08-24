using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Collection;

// =====================================================================================
// UI tests for the CollectionView on GridCollectionDemoView.
// Written against the PROPOSED bases - see container-and-collection-design.md.
// Target: testsnew/Brinell.Maui.UITests2/Tests2/Collection/ProductCollectionTests.cs
//
// STAGED - not yet part of the codebase. Move to the destination above only on an
// explicit instruction to start implementing. See ../README.md#destinations-when-implementing.
// =====================================================================================

/// <summary>
/// Tests for CollectionView item scoping, retrieval, and virtualization.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "CollectionView")]
public class ProductCollectionTests
{
    private readonly MauiFixture _fixture;
    private GridCollectionDemoPage Page => _fixture.GridCollectionDemoPage;

    public ProductCollectionTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToGridCollectionDemo();
    }

    #region Item retrieval

    /// <summary>
    /// Retrieving a list item is the headline requirement.
    /// </summary>
    [Fact]
    [Trait("Method", "Item")]
    public void Item_ReturnsTypedRow()
    {
        ProductRow row = Page.Products.Item(0);

        Assert.NotNull(row);
        Assert.Equal(0, row.Index);
        row.Name.AssertExists();
    }

    /// <summary>
    /// The indexer is an equivalent spelling of Item(i). Both are supported.
    /// </summary>
    [Fact]
    [Trait("Method", "Indexer")]
    public void Indexer_MatchesItem()
    {
        Assert.Equal(Page.Products.Item(1).Name.GetText(),
                     Page.Products[1].Name.GetText());
    }

    /// <summary>
    /// TryItem returns null rather than throwing for an out-of-range index.
    /// </summary>
    [Fact]
    [Trait("Method", "TryItem")]
    public void TryItem_OutOfRange_ReturnsNull()
        => Assert.Null(Page.Products.TryItem(9999));

    [Fact]
    [Trait("Method", "Item")]
    public void Item_OutOfRange_Throws()
        => Assert.Throws<ElementNotFoundException>(() => Page.Products.Item(9999));

    #endregion

    #region Item scoping - the core of the design

    /// <summary>
    /// THE critical test. Every row uses the SAME AutomationId ("ProductNameLabel"),
    /// so this can only pass if each row is scoped to its own subtree.
    ///
    /// The equivalent test on the old List (ListItems_AreIndependentlyScoped in
    /// ContainerScopingTests) is [Fact(Skip = ...)] because item scoping does not
    /// actually work there - items resolve against the page, not the row.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void Rows_WithRepeatingIds_AreIndependentlyScoped()
    {
        var name0 = Page.Products.Item(0).Name.GetText();
        var name1 = Page.Products.Item(1).Name.GetText();
        var name2 = Page.Products.Item(2).Name.GetText();

        Assert.Equal("Keyboard", name0);
        Assert.Equal("Mouse", name1);
        Assert.Equal("Monitor", name2);
    }

    /// <summary>
    /// Scoping holds for every control in the template, not just the first.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void Row_AllTemplateControls_ResolveWithinTheRow()
    {
        var row = Page.Products.Item(2);

        row.Name.AssertText("Monitor");
        row.Stock.AssertText("Out of stock");
        row.Selected.AssertExists();
        row.Delete.AssertExists();
    }

    /// <summary>
    /// Acting on one row must not affect another.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void Row_Interaction_DoesNotLeakToOtherRows()
    {
        Page.Products.Item(0).Selected.SetChecked(true);

        Assert.True(Page.Products.Item(0).Selected.IsChecked());
        Assert.False(Page.Products.Item(1).Selected.IsChecked());
    }

    /// <summary>
    /// A row does not resolve controls that live outside it.
    /// </summary>
    [Fact]
    [Trait("Pattern", "NoParentFallback")]
    public void Row_DoesNotFindCollectionLevelControls()
    {
        var countInRow = new Label<ProductRow>(Page.Products.Item(0), "ProductCountLabel");

        Page.Products.CountLabel.AssertExists();
        Assert.False(countInRow.IsExists());
    }

    #endregion

    #region Collection as a scope

    /// <summary>
    /// A collection is also a container, so it holds its own controls.
    /// </summary>
    [Fact]
    [Trait("Pattern", "CollectionIsScope")]
    public void Collection_HoldsItsOwnControls()
    {
        Page.Products.Title.AssertExists();
        Page.Products.CountLabel.AssertExists();
        Page.Products.ClearButton.AssertExists();
    }

    /// <summary>
    /// The empty-view label is a collection-level control, shown only when empty.
    /// </summary>
    [Fact]
    [Trait("Pattern", "EmptyView")]
    public void Collection_EmptyLabel_AppearsWhenCleared()
    {
        Assert.False(Page.Products.IsEmpty());

        Page.Products.ClearButton.Click();

        Assert.True(Page.Products.WaitItemCount(0, 3000));
        Page.Products.EmptyLabel.AssertVisible(true);
        Page.Products.AssertEmpty(true);
    }

    #endregion

    #region Counting

    [Fact]
    [Trait("Method", "GetItemCount")]
    public void GetItemCount_ReturnsSeededCount()
        => Assert.Equal(3, Page.Products.GetItemCount());

    [Fact]
    [Trait("Method", "AssertItemCount")]
    public void AssertItemCount_ReturnsCollectionForChaining()
    {
        ProductCollection result = Page.Products.AssertItemCount(3);

        Assert.Same(Page.Products, result);
    }

    /// <summary>
    /// Count reflects mutations without any reindexing on the app side.
    /// </summary>
    [Fact]
    [Trait("Method", "WaitItemCount")]
    public void DeletingRow_DecrementsCount()
    {
        var before = Page.Products.GetItemCount();

        Page.Products.Item(0).Delete.Click();

        Assert.True(Page.Products.WaitItemCount(before - 1, 3000));
    }

    /// <summary>
    /// After a delete, the remaining rows shift up and stay correctly scoped.
    /// This is what ReindexTasks() had to fake on the old design.
    /// </summary>
    [Fact]
    [Trait("Pattern", "ItemScoping")]
    public void DeletingRow_RemainingRowsReindexNaturally()
    {
        Page.Products.Item(0).Delete.Click();
        Page.Products.WaitItemCount(2, 3000);

        Assert.Equal("Mouse", Page.Products.Item(0).Name.GetText());
        Assert.Equal("Monitor", Page.Products.Item(1).Name.GetText());
    }

    #endregion

    #region Search by content

    [Fact]
    [Trait("Method", "ItemWhere")]
    public void ItemWhere_FindsRowByName()
    {
        var row = Page.Products.ItemWhere(r => r.Name.GetText() == "Monitor");

        Assert.Equal(2, row.Index);
    }

    [Fact]
    [Trait("Method", "FindItem")]
    public void FindItem_ReturnsNullWhenNoMatch()
        => Assert.Null(Page.Products.FindItem(r => r.Name.GetText() == "Nonexistent"));

    [Fact]
    [Trait("Method", "ItemWhere")]
    public void ItemWhere_ThrowsWhenNoMatch()
        => Assert.Throws<ElementNotFoundException>(
            () => Page.Products.ItemWhere(r => r.Name.GetText() == "Nonexistent"));

    /// <summary>
    /// Domain helper on the collection reads naturally at the call site.
    /// </summary>
    [Fact]
    [Trait("Pattern", "DomainHelper")]
    public void ByName_DeletesTheRightRow()
    {
        Page.Products.ByName("Mouse").Delete.Click();

        Page.Products.WaitItemCount(2, 3000);
        Assert.Null(Page.Products.FindItem(r => r.Name.GetText() == "Mouse"));
    }

    #endregion

    #region Items enumeration

    [Fact]
    [Trait("Property", "Items")]
    public void ToList_ReturnsEveryRow()
    {
        var rows = Page.Products.ToList();

        Assert.Equal(Page.Products.GetItemCount(), rows.Count);
        Assert.All(rows, r => r.Name.AssertExists());
    }

    [Fact]
    [Trait("Property", "Items")]
    public void Items_AreOrderedByIndex()
    {
        var rows = Page.Products.ToList();

        for (var i = 0; i < rows.Count; i++)
        {
            Assert.Equal(i, rows[i].Index);
        }
    }

    /// <summary>
    /// Items is lazy (design 8.2), so a match early in a large collection is found
    /// without materializing the whole thing.
    /// </summary>
    [Fact]
    [Trait("Property", "Items")]
    public void Items_IsLazy_FirstMatchStopsEarly()
    {
        Page.Products.BulkAddButton.Click();
        Page.Products.WaitItemCount(63, 5000);

        var first = Page.Products.Items.First(r => r.Name.GetText() == "Keyboard");

        Assert.Equal(0, first.Index);
    }

    #endregion

    #region Selection

    [Fact]
    [Trait("Method", "SelectItem")]
    public void SelectItem_ReturnsCollectionForChaining()
    {
        ProductCollection result = Page.Products.SelectItem(1);

        Assert.Same(Page.Products, result);
    }

    #endregion

    #region Virtualization

    /// <summary>
    /// With 60+ rows the CollectionView virtualizes. ScrollToItem must materialize a
    /// row that is not initially in the automation tree, using scroll-and-observe
    /// rather than a fixed sleep.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Virtualization")]
    public void ScrollToItem_MaterializesOffscreenRow()
    {
        Page.Products.BulkAddButton.Click();
        Page.Products.WaitItemCount(63, 5000);

        Page.Products.ScrollToItem(60);

        Page.Products.Item(60).Name.AssertExists();
    }

    /// <summary>
    /// ItemWhere scrolls while searching, so it finds a row that starts off-screen.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Virtualization")]
    public void ItemWhere_ScrollsToFindOffscreenRow()
    {
        Page.Products.BulkAddButton.Click();
        Page.Products.WaitItemCount(63, 5000);

        var row = Page.Products.ItemWhere(r => r.Name.GetText() == "Bulk Product 55");

        Assert.NotNull(row);
    }

    /// <summary>
    /// Counting a virtualized collection reports materialized rows. This test documents
    /// that contract explicitly rather than asserting an exact number.
    /// </summary>
    [Fact]
    [Trait("Pattern", "Virtualization")]
    public void GetItemCount_OnVirtualizedCollection_ReportsMaterializedRows()
    {
        Page.Products.BulkAddButton.Click();

        Assert.True(Page.Products.WaitAnyItem(5000));
        Assert.True(Page.Products.GetItemCount() > 0);
    }

    #endregion

    #region Fluent return

    /// <summary>
    /// A control action inside a row returns the ROW, and Parent steps out to the
    /// collection, then to the page.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void RowAction_ReturnsRow_ParentReturnsCollection()
    {
        var row = Page.Products.Item(0);

        ProductRow afterCheck = row.Selected.SetChecked(true);
        ProductCollection collection = afterCheck.Parent;
        GridCollectionDemoPage page = collection.Parent;

        Assert.Same(row, afterCheck);
        Assert.Same(Page.Products, collection);
        Assert.Same(Page, page);
    }

    #endregion
}
