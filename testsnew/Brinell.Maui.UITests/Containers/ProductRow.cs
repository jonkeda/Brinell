using Brinell.Maui.Containers;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// One product row, scoped to its own item root.
/// </summary>
/// <remarks>
/// Every automation id below repeats unchanged on every row of the collection. A row
/// receives an already-discovered root element and its index; it never locates itself
/// from the page by a unique id.
/// </remarks>
public class ProductRow : ItemContainerBase<ProductCollection, ProductRow>
{
    public ProductRow(ProductCollection collection, IMauiElement itemRoot, int index)
        : base(collection, itemRoot, index)
    {
    }

    /// <summary>The row's selection checkbox.</summary>
    public CheckBox<ProductRow> Selected => new(this, "ProductSelectedCheckBox");

    /// <summary>The product name label.</summary>
    public Label<ProductRow> Name => new(this, "ProductNameLabel");

    /// <summary>The product price label.</summary>
    public Label<ProductRow> Price => new(this, "ProductPriceLabel");

    /// <summary>The stock-state label.</summary>
    public Label<ProductRow> Stock => new(this, "ProductStockLabel");

    /// <summary>The delete button for this row.</summary>
    public Button<ProductRow> Delete => new(this, "ProductDeleteButton");
}
