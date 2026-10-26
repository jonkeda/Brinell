using Microsoft.AspNetCore.Components;

namespace Bruinet.Html.Bunit.Tests;

/// <summary>A service an app registers; the context finds it in <see cref="BunitTestContextOptions.FallbackServices"/>.</summary>
public interface IGreeting
{
    string Text { get; }
}

public sealed record FixedGreeting(string Text) : IGreeting;

/// <summary>A greeting that takes the circuit's navigation manager, as a scoped app service may.</summary>
public sealed class NavigatingGreeting(NavigationManager navigation) : IGreeting
{
    public string Text => navigation.Uri;
}

/// <summary>An app host's navigation manager, which is not a circuit's.</summary>
public sealed class HostNavigationManager : NavigationManager
{
    public HostNavigationManager() => Initialize("http://host/", "http://host/");
}
