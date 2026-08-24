using Brinell.Maui.UITests.Pages2;

namespace Brinell.Maui.UITests.Tests.Container;

// =====================================================================================
// UI tests for the Grid container on GridCollectionDemoView.
// Written against the PROPOSED bases - see container-and-collection-design.md.
// Target: testsnew/Brinell.Maui.UITests2/Tests2/Container/GridContainerTests.cs
//
// STAGED - not yet part of the codebase. Move to the destination above only on an
// explicit instruction to start implementing. See ../README.md#destinations-when-implementing.
// =====================================================================================

/// <summary>
/// Tests for Grid-based container scoping and fluent behaviour.
/// Uses xUnit Assert per SPEC-017b design principles (never FluentAssertions).
/// </summary>
[Collection("Maui")]
[Trait("Category", "UITest")]
[Trait("Pattern", "GridContainer")]
public class GridContainerTests
{
    private readonly MauiFixture _fixture;
    private GridCollectionDemoPage Page => _fixture.GridCollectionDemoPage;

    public GridContainerTests(MauiFixture fixture)
    {
        _fixture = fixture;
        _fixture.NavigateToGridCollectionDemo();
    }

    #region Scoping

    /// <summary>
    /// The Grid container finds its own children.
    /// </summary>
    [Fact]
    [Trait("Method", "FindElement")]
    public void ProductForm_FindsItsOwnControls()
    {
        Page.ProductForm.Title.AssertExists();
        Page.ProductForm.NameEntry.AssertExists();
        Page.ProductForm.PriceEntry.AssertExists();
        Page.ProductForm.AddButton.AssertExists();
    }

    /// <summary>
    /// A container must NOT fall back to the parent scope. A control that exists on the
    /// page but outside this container is not findable through it.
    /// </summary>
    [Fact]
    [Trait("Pattern", "NoParentFallback")]
    public void ProductForm_DoesNotFindControlsOutsideItself()
    {
        // PageTitle exists on the page, but not inside the product form.
        var titleInForm = new Label<ProductFormContainer>(Page.ProductForm, "PageTitle");

        Page.PageTitle.AssertExists();          // exists at page level
        Assert.False(titleInForm.IsExists());   // but not within the container
    }

    /// <summary>
    /// Nested container: a Grid inside a Grid scopes one level deeper.
    /// </summary>
    [Fact]
    [Trait("Pattern", "NestedScoping")]
    public void ProductOptions_ScopesInsideProductForm()
    {
        Page.ProductForm.Options.AssertExists();
        Page.ProductForm.Options.InStockCheckBox.AssertExists();
        Page.ProductForm.Options.InStockCaption.AssertExists();
    }

    /// <summary>
    /// The nested container does not see its parent's siblings.
    /// </summary>
    [Fact]
    [Trait("Pattern", "NestedScoping")]
    public void ProductOptions_DoesNotFindParentControls()
    {
        var addButtonInOptions =
            new Button<ProductOptionsContainer>(Page.ProductForm.Options, "ProductAddButton");

        Page.ProductForm.AddButton.AssertExists();
        Assert.False(addButtonInOptions.IsExists());
    }

    #endregion

    #region Fluent return type

    /// <summary>
    /// This is the behaviour ContainerObjectBase exists to provide: a control action
    /// inside a container returns the CONTAINER, so chains stay in scope.
    /// Under the old ContainerBase these returned the page instead.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void ControlAction_ReturnsContainer_NotPage()
    {
        ProductFormContainer result = Page.ProductForm.NameEntry.SetText("Widget");

        Assert.Same(Page.ProductForm, result);
    }

    /// <summary>
    /// The container's own assertions also return the container.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentReturn")]
    public void ContainerAssertion_ReturnsContainer()
    {
        ProductFormContainer result = Page.ProductForm.AssertVisible(true);

        Assert.Same(Page.ProductForm, result);
    }

    /// <summary>
    /// Parent is the single explicit way back out to the page.
    /// </summary>
    [Fact]
    [Trait("Property", "Parent")]
    public void Parent_ReturnsOwningPage()
    {
        GridCollectionDemoPage page = Page.ProductForm.Parent;

        Assert.Same(Page, page);
    }

    /// <summary>
    /// Parent chains up through nested containers one level at a time.
    /// </summary>
    [Fact]
    [Trait("Property", "Parent")]
    public void Parent_ChainsUpThroughNestedContainers()
    {
        ProductFormContainer form = Page.ProductForm.Options.Parent;
        GridCollectionDemoPage page = Page.ProductForm.Options.Parent.Parent;

        Assert.Same(Page.ProductForm, form);
        Assert.Same(Page, page);
    }

    /// <summary>
    /// A full chain: work inside the container, then step out and continue on the page.
    /// </summary>
    [Fact]
    [Trait("Pattern", "FluentChain")]
    public void FluentChain_StaysInContainerThenExits()
    {
        Page.ProductForm
                .FillProduct("Chained Widget", "12.34", inStock: true)
                .AddButton.Click()
                .Parent
            .Products
                .AssertItemCount(4);
    }

    #endregion

    #region Container state

    [Fact]
    [Trait("Method", "IsExists")]
    public void ProductForm_Exists() => Assert.True(Page.ProductForm.IsExists());

    [Fact]
    [Trait("Method", "IsReady")]
    public void ProductForm_IsReady_WhenPageLoadedAndRootPresent()
        => Assert.True(Page.ProductForm.IsReady());

    /// <summary>
    /// Cache invalidation must not break subsequent lookups.
    /// </summary>
    [Fact]
    [Trait("Method", "InvalidateCache")]
    public void InvalidateCache_ControlsStillResolve()
    {
        Page.ProductForm.Title.AssertExists();

        Page.ProductForm.InvalidateCache();

        Page.ProductForm.Title.AssertExists();
    }

    #endregion

    #region Interaction

    /// <summary>
    /// Adding through the Grid form appends a row to the collection.
    /// </summary>
    [Fact]
    [Trait("Method", "Click")]
    public void AddProduct_AppendsRowToCollection()
    {
        var before = Page.Products.GetItemCount();

        Page.ProductForm
            .FillProduct("Test Product", "9.99", inStock: true)
            .AddButton.Click();

        Assert.True(Page.Products.WaitItemCount(before + 1, 3000));
    }

    /// <summary>
    /// The nested checkbox is independently settable.
    /// </summary>
    [Fact]
    [Trait("Method", "SetChecked")]
    public void ProductOptions_CheckBox_Toggles()
    {
        Page.ProductForm.Options.InStockCheckBox.SetChecked(false);
        Assert.False(Page.ProductForm.Options.InStockCheckBox.IsChecked());

        Page.ProductForm.Options.InStockCheckBox.SetChecked(true);
        Assert.True(Page.ProductForm.Options.InStockCheckBox.IsChecked());
    }

    #endregion
}
