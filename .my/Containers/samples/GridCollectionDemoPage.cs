using Brinell.Maui.Controls.Base;
using Brinell.Maui.Controls.Collection;
using Brinell.Maui.Controls.Container;

namespace Brinell.Maui.UITests.Pages2;

// =====================================================================================
// Page objects for GridCollectionDemoView, written against the PROPOSED bases in
// container-and-collection-design.md. These do not compile until migration steps 1-5
// land. They are the reference for what the API should feel like.
//
// Target: split across
//   testsnew/Brinell.Maui.UITests2/Pages2/GridCollectionDemoPage.cs   (the page)
//   testsnew/Brinell.Maui.UITests2/Containers2/*.cs                   (one file per container)
//
// STAGED - not yet part of the codebase. Move to the destinations above only on an
// explicit instruction to start implementing. See ../README.md#destinations-when-implementing.
// =====================================================================================

/// <summary>
/// Page object for the Grid + CollectionView demo.
/// </summary>
public class GridCollectionDemoPage : PageObjectBase<GridCollectionDemoPage>
{
    public GridCollectionDemoPage(IMauiTestContext context)
        : base(context)
    {
        // Containers and collections are constructed once, not as => new() properties,
        // so the cached ContainerRoot survives across calls.
        ProductForm = new ProductFormContainer(this, "ProductFormContainer");
        Products = new ProductCollection(this, "ProductListContainer");
    }

    /// <inheritdoc />
    public override string Name => "GridCollectionDemoPage";

    /// <inheritdoc />
    public override bool IsLoaded(int? timeoutMs = null) => PageTitle.IsExists(timeoutMs);

    /// <summary>The page title label.</summary>
    public Label<GridCollectionDemoPage> PageTitle => new(this, "PageTitle");

    /// <summary>The Grid-based product entry form.</summary>
    public ProductFormContainer ProductForm { get; }

    /// <summary>The product CollectionView.</summary>
    public ProductCollection Products { get; }
}

/// <summary>
/// The Grid container holding the product entry form.
/// </summary>
/// <remarks>
/// Derives from <c>ContainerObjectBase</c>, NOT <c>ContainerBase</c>. The difference is the
/// fluent return type: every action here returns <see cref="ProductFormContainer"/>, so a
/// chain stays inside the container until <c>.Parent</c> is called explicitly.
/// </remarks>
public class ProductFormContainer : ContainerObjectBase<GridCollectionDemoPage, ProductFormContainer>
{
    public ProductFormContainer(IMauiScope<GridCollectionDemoPage> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    public Label<ProductFormContainer> Title => new(this, "ProductFormTitle");
    public Entry<ProductFormContainer> NameEntry => new(this, "ProductNameEntry");
    public Entry<ProductFormContainer> PriceEntry => new(this, "ProductPriceEntry");
    public Button<ProductFormContainer> AddButton => new(this, "ProductAddButton");

    /// <summary>Nested container - a Grid inside the Grid.</summary>
    public ProductOptionsContainer Options => new(this, "ProductOptionsContainer");

    /// <summary>
    /// Fills the form. Returns this container so the caller stays in scope.
    /// Domain helpers like this belong on the container, per AGENTS.md
    /// ("put repeated interaction behavior in Brinell controls").
    /// </summary>
    public ProductFormContainer FillProduct(string name, string price, bool inStock)
    {
        NameEntry.SetText(name);
        PriceEntry.SetText(price);
        Options.InStockCheckBox.SetChecked(inStock);
        return Self;
    }
}

/// <summary>
/// Options sub-grid nested inside <see cref="ProductFormContainer"/>.
/// Its parent is a container, not a page - which is the point.
/// </summary>
public class ProductOptionsContainer : ContainerObjectBase<ProductFormContainer, ProductOptionsContainer>
{
    public ProductOptionsContainer(IMauiScope<ProductFormContainer> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    public CheckBox<ProductOptionsContainer> InStockCheckBox => new(this, "ProductInStockCheckBox");
    public Label<ProductOptionsContainer> InStockCaption => new(this, "ProductInStockCaption");
}

/// <summary>
/// The product CollectionView.
/// </summary>
/// <remarks>
/// A collection is a container that also hands out items, so it can hold its own controls
/// (<see cref="EmptyLabel"/>, <see cref="CountLabel"/>) alongside <c>Item(i)</c>.
/// Items are discovered by the "ProductRow" locator - the id repeats on every row.
/// </remarks>
public class ProductCollection : CollectionObjectBase<GridCollectionDemoPage, ProductCollection, ProductRow>
{
    public ProductCollection(IMauiScope<GridCollectionDemoPage> parentScope, string automationId)
        : base(parentScope,
               automationId,
               ItemStrategy.ByLocator(Locator.ByAutomationId("ProductRow")),
               (collection, itemRoot, index) => new ProductRow(collection, itemRoot, index))
    {
    }

    // Controls that belong to the collection itself, not to any row.
    public Label<ProductCollection> Title => new(this, "ProductListTitle");
    public Label<ProductCollection> EmptyLabel => new(this, "ProductListEmptyLabel");
    public Label<ProductCollection> CountLabel => new(this, "ProductCountLabel");
    public Button<ProductCollection> ClearButton => new(this, "ProductClearButton");
    public Button<ProductCollection> BulkAddButton => new(this, "ProductBulkAddButton");

    /// <summary>Finds a row by its product name.</summary>
    public ProductRow ByName(string name) => ItemWhere(row => row.Name.GetText() == name);
}

/// <summary>
/// One product row. Scoped to its own item root, so every control id below repeats
/// unchanged on every row of the CollectionView.
/// </summary>
public class ProductRow : ItemContainerBase<ProductCollection, ProductRow>
{
    public ProductRow(ProductCollection collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    public CheckBox<ProductRow> Selected => new(this, "ProductSelectedCheckBox");
    public Label<ProductRow> Name => new(this, "ProductNameLabel");
    public Label<ProductRow> Price => new(this, "ProductPriceLabel");
    public Label<ProductRow> Stock => new(this, "ProductStockLabel");
    public Button<ProductRow> Delete => new(this, "ProductDeleteButton");
}
