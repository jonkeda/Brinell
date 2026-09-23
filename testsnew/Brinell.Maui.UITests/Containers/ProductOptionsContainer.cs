using Brinell.Maui.Containers;

namespace Brinell.Maui.UITests.Containers;

/// <summary>
/// The options sub-grid nested inside <see cref="ProductFormContainer"/>.
/// Its parent is a container, not a page - which is the point of the nesting test.
/// </summary>
public class ProductOptionsContainer : ContainerObjectBase<ProductFormContainer, ProductOptionsContainer>
{
    public ProductOptionsContainer(IMauiScope<ProductFormContainer> parentScope, string automationId)
        : base(parentScope, automationId)
    {
    }

    /// <summary>The in-stock checkbox.</summary>
    public CheckBox<ProductOptionsContainer> InStockCheckBox => new(this, "ProductInStockCheckBox");

    /// <summary>The in-stock caption label.</summary>
    public Label<ProductOptionsContainer> InStockCaption => new(this, "ProductInStockCaption");
}
