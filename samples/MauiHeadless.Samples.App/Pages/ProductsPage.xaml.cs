using Microsoft.Maui.Controls;
using MauiHeadless.Samples.App.Services;

namespace MauiHeadless.Samples.App.Pages;

/// <summary>
/// A list that loads in OnAppearing (an async void handler, as apps write it), and pushes the
/// product's page when one is chosen.
/// </summary>
public partial class ProductsPage : ContentPage
{
    private readonly IProductCatalog _catalog;

    public ProductsPage(IProductCatalog catalog)
    {
        InitializeComponent();
        _catalog = catalog;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async void OnReload(object? sender, EventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        Summary.Text = "Loading...";
        var products = await _catalog.GetProductsAsync();
        List.ItemsSource = products;
        Summary.Text = products.Count == 1 ? "1 product" : $"{products.Count} products";
    }

    private async void OnSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not Product product)
            return;

        List.SelectedItem = null;
        await Navigation.PushAsync(new ProductPage(product));
    }
}
