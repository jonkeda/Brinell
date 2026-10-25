namespace MauiUnit.Samples.App.Services;

public sealed record Product(string Id, string Name, decimal Price);

/// <summary>What the shop sells; a real app's would call a server.</summary>
public interface IProductCatalog
{
    Task<IReadOnlyList<Product>> GetProductsAsync();
}
