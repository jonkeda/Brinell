using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Bruinet.Html.Bunit;

/// <summary>
/// The app's own services behind bUnit's (<see cref="BunitTestContextOptions.FallbackServices"/>), refusing a service that
/// takes a circuit's service: there it would get the app host's navigation manager, script runtime or authentication, which
/// no circuit initialized, and fail later with a message that names neither. Registered again in the context's own
/// services, it takes bUnit's.
/// </summary>
internal sealed class CircuitServiceGuard(IServiceProvider inner) : IServiceProvider
{
    private static readonly Type[] CircuitServices = [typeof(NavigationManager), typeof(IJSRuntime), typeof(AuthenticationStateProvider)];

    // Per implementation, the circuit service its constructor takes, or null.
    private static readonly ConcurrentDictionary<Type, Type?> Taken = new();

    public object? GetService(Type serviceType)
    {
        var service = inner.GetService(serviceType);
        if (service is not null && Taken.GetOrAdd(service.GetType(), CircuitServiceTakenBy) is { } circuit)
        {
            throw new InvalidOperationException(
                $"{serviceType.Name} ({service.GetType().Name}) comes from the app's own services and takes {circuit.Name}, which there is the app host's, " +
                $"not this circuit's. Register {serviceType.Name} again in the context's ConfigureServices, so it takes bUnit's.");
        }

        return service;
    }

    private static Type? CircuitServiceTakenBy(Type implementation)
        => implementation.GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => CircuitServices.FirstOrDefault(circuit => circuit.IsAssignableFrom(parameter.ParameterType)))
            .FirstOrDefault(circuit => circuit is not null);
}
