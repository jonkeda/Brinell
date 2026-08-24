// =====================================================================================
// Target: samples/Brinell.Samples.Maui.App2/ViewModels2/GridCollectionDemoViewModel.cs
//
// STAGED - not yet part of the codebase. Move to the destination above only on an
// explicit instruction to start implementing. See ../README.md#destinations-when-implementing.
// =====================================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Brinell.Samples.Maui.App.ViewModels2;

/// <summary>
/// ViewModel for the Grid + CollectionView demo page.
/// </summary>
/// <remarks>
/// Contrast with <see cref="ContainerDemoViewModel"/>: that one carries an
/// <c>AutomationId =&gt; $"Task_{_id}"</c> property and a <c>ReindexTasks()</c> call after
/// every mutation, purely so each row gets a globally unique id for the test framework
/// to find it. That machinery exists only to work around the item-scoping defect
/// (container-and-collection-design.md 3.2).
///
/// <see cref="ProductItem"/> below has no Id, no AutomationId, and no reindexing. Under
/// CollectionObjectBase each row is scoped to its own root, so the view model is free to
/// be a plain model again. Removing that burden from app authors is a real benefit of
/// this design, not just a test-side convenience.
/// </remarks>
public class GridCollectionDemoViewModel : ParentViewModel
{
    private string _newProductName = "";
    private string _newProductPrice = "";
    private bool _newProductInStock = true;
    private ProductItem? _selectedProduct;

    public GridCollectionDemoViewModel()
    {
        Products = new ObservableCollection<ProductItem>
        {
            new() { Name = "Keyboard", Price = 49.99m, InStock = true },
            new() { Name = "Mouse", Price = 24.50m, InStock = true },
            new() { Name = "Monitor", Price = 199.00m, InStock = false }
        };

        Products.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ProductCount));
            OnPropertyChanged(nameof(IsProductListEmpty));
        };

        AddProductCommand = new AsyncRelayCommand(this, AddProductAsync);
        DeleteProductCommand = new RelayCommand<ProductItem>(DeleteProduct);
        ClearProductsCommand = new RelayCommand<object>(_ => Products.Clear());
        BulkAddProductsCommand = new RelayCommand<object>(_ => BulkAddProducts());
    }

    #region Grid form section

    public string NewProductName
    {
        get => _newProductName;
        set => SetProperty(ref _newProductName, value);
    }

    public string NewProductPrice
    {
        get => _newProductPrice;
        set => SetProperty(ref _newProductPrice, value);
    }

    public bool NewProductInStock
    {
        get => _newProductInStock;
        set => SetProperty(ref _newProductInStock, value);
    }

    public IAsyncRelayCommand AddProductCommand { get; }

    private async Task AddProductAsync()
    {
        if (string.IsNullOrWhiteSpace(NewProductName)) return;

        await Task.Delay(50);

        _ = decimal.TryParse(NewProductPrice, out var price);
        Products.Add(new ProductItem
        {
            Name = NewProductName,
            Price = price,
            InStock = NewProductInStock
        });

        NewProductName = "";
        NewProductPrice = "";
    }

    #endregion

    #region Collection section

    public ObservableCollection<ProductItem> Products { get; }

    public ProductItem? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public int ProductCount => Products.Count;

    public bool IsProductListEmpty => Products.Count == 0;

    public ICommand DeleteProductCommand { get; }
    public ICommand ClearProductsCommand { get; }
    public ICommand BulkAddProductsCommand { get; }

    private void DeleteProduct(ProductItem? product)
    {
        if (product == null) return;
        Products.Remove(product);
        // No reindexing needed - rows are scoped, not id-addressed.
    }

    /// <summary>
    /// Adds enough rows that the CollectionView must virtualize, so
    /// ScrollToItem and the scroll-and-observe loop have a real target.
    /// </summary>
    private void BulkAddProducts()
    {
        for (var i = 0; i < 60; i++)
        {
            Products.Add(new ProductItem
            {
                Name = $"Bulk Product {i:D2}",
                Price = 10m + i,
                InStock = i % 3 != 0
            });
        }
    }

    #endregion
}

/// <summary>
/// A product row. Deliberately has no Id or AutomationId property -
/// item scoping makes them unnecessary.
/// </summary>
public class ProductItem : INotifyPropertyChanged
{
    private string _name = "";
    private decimal _price;
    private bool _inStock;
    private bool _isSelected;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public decimal Price
    {
        get => _price;
        set { _price = value; OnPropertyChanged(); OnPropertyChanged(nameof(PriceText)); }
    }

    public bool InStock
    {
        get => _inStock;
        set { _inStock = value; OnPropertyChanged(); OnPropertyChanged(nameof(StockText)); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string PriceText => Price.ToString("C");

    public string StockText => InStock ? "In stock" : "Out of stock";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
