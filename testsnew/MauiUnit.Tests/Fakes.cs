using MauiUnit.Samples.App.Services;

namespace MauiUnit.Tests;

/// <summary>A settings store the test controls: it records saves, can fail, and can hold a save open.</summary>
internal sealed class FakeSettingsService : ISettingsService
{
    public string? Name { get; set; }

    public bool Notifications { get; set; } = true;

    public List<(string Name, bool Notifications)> Saves { get; } = [];

    public Exception? Failure { get; set; }

    /// <summary>When set, a save waits for this before it completes.</summary>
    public TaskCompletionSource? Gate { get; set; }

    public string? LoadName() => Name;

    public bool LoadNotifications() => Notifications;

    public async Task SaveAsync(string name, bool notifications)
    {
        if (Gate is not null)
            await Gate.Task;
        if (Failure is not null)
            throw Failure;
        Saves.Add((name, notifications));
    }
}

internal sealed class FakeCatalog(params Product[] products) : IProductCatalog
{
    public List<Product> Products { get; } = [.. products];

    public int Calls { get; private set; }

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        Calls++;
        // A real catalog awaits the network; yielding sends the rest through the dispatcher's queue.
        await Task.Yield();
        return Products.ToList();
    }
}
