using Brinell.Maui.Containers;
using Brinell.Maui.UITests.Pages;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// The Grid container holding the product entry form.
/// </summary>
/// <remarks>
/// Derives from <see cref="ContainerObjectBase{TParent, TSelf}"/>, so every member here
/// returns <see cref="ProductFormContainer"/> and a chain stays inside the container
/// until <c>Parent</c> is called.
/// </remarks>
public class ProductFormContainer : ContainerObjectBase<GridCollectionDemoPage, ProductFormContainer>
{
    public ProductFormContainer(IMauiScope<GridCollectionDemoPage> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    /// <summary>The form heading.</summary>
    public Label<ProductFormContainer> Title => new(this, "ProductFormTitle");

    /// <summary>The product name entry.</summary>
    public Entry<ProductFormContainer> NameEntry => new(this, "ProductNameEntry");

    /// <summary>The product price entry.</summary>
    public Entry<ProductFormContainer> PriceEntry => new(this, "ProductPriceEntry");

    /// <summary>The add-product button.</summary>
    public Button<ProductFormContainer> AddButton => new(this, "ProductAddButton");

    /// <summary>The nested options container - a Grid inside the Grid.</summary>
    public ProductOptionsContainer Options => new(this, "ProductOptionsContainer");

    /// <summary>
    /// Fills the form and returns this container so the caller stays in scope.
    /// </summary>
    public ProductFormContainer FillProduct(string name, string price, bool inStock)
    {
        NameEntry.SetText(name);
        PriceEntry.SetText(price);
        Options.InStockCheckBox.SetChecked(inStock);
        return Self;
    }
}
