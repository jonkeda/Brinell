using Microsoft.Maui.Controls;
using MauiUnit.Samples.App.Services;

namespace MauiUnit.Samples.App.Pages;

/// <summary>One product: a page built in C#, as some apps build theirs.</summary>
public sealed class ProductPage : ContentPage
{
    public ProductPage(Product product)
    {
        Product = product;
        AutomationId = "ProductPage";
        Title = product.Name;
        Content = new VerticalStackLayout
        {
            new Label { AutomationId = "Title", Text = product.Name },
            new Label { AutomationId = "Price", Text = product.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) },
        };
    }

    public Product Product { get; }
}
