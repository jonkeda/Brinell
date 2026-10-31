using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MauiHeadless.Hosting;

/// <summary>
/// The test's services: a service collection until the first element is rendered, then the provider
/// built from it. Registrations after that point are refused, as in bUnit.
/// </summary>
public sealed class TestServiceProvider : IServiceCollection, IServiceProvider, IDisposable
{
    private readonly ServiceCollection _services = [];
    private ServiceProvider? _provider;

    /// <summary>Whether the provider has been built, which fixes the registrations.</summary>
    public bool IsProviderBuilt => _provider is not null;

    /// <summary>Replaces every registration of <typeparamref name="TService"/> with one instance: a fake or a mock.</summary>
    public TestServiceProvider Replace<TService>(TService instance) where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        EnsureOpen();
        _services.RemoveAll<TService>();
        _services.AddSingleton(instance);
        return this;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType) => Build().GetService(serviceType);

    /// <summary>Builds the provider, which fixes the registrations; a no-op once built.</summary>
    internal IServiceProvider Build()
        => _provider ??= _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    public void Dispose() => _provider?.Dispose();

    #region IServiceCollection

    public ServiceDescriptor this[int index]
    {
        get => _services[index];
        set { EnsureOpen(); _services[index] = value; }
    }

    public int Count => _services.Count;

    public bool IsReadOnly => IsProviderBuilt;

    public void Add(ServiceDescriptor item) { EnsureOpen(); _services.Add(item); }

    public void Clear() { EnsureOpen(); _services.Clear(); }

    public bool Contains(ServiceDescriptor item) => _services.Contains(item);

    public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _services.CopyTo(array, arrayIndex);

    public IEnumerator<ServiceDescriptor> GetEnumerator() => _services.GetEnumerator();

    public int IndexOf(ServiceDescriptor item) => _services.IndexOf(item);

    public void Insert(int index, ServiceDescriptor item) { EnsureOpen(); _services.Insert(index, item); }

    public bool Remove(ServiceDescriptor item) { EnsureOpen(); return _services.Remove(item); }

    public void RemoveAt(int index) { EnsureOpen(); _services.RemoveAt(index); }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    private void EnsureOpen()
    {
        if (IsProviderBuilt)
            throw new InvalidOperationException(
                "The services are fixed once the first element is rendered or a service resolved. Register them before the first Render.");
    }
}
