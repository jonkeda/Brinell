using Brinell.Html.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Brinell.Html.Bunit;

/// <summary>What a <see cref="BunitTestContext"/> renders and with which services.</summary>
public sealed class BunitTestContextOptions : HtmlTestContextOptions
{
    /// <summary>The component rendered as the app: a suite's <c>Routes</c>, or one component on its own.</summary>
    public required Type RootComponent { get; init; }

    /// <summary>The app's services, registered by the suite before the root renders.</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>The signed-in user's name for bUnit's test authorization; null registers none.</summary>
    public string? User { get; init; }
}
