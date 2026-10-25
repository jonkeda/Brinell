using Bruinell.Core.Exceptions;

namespace Bruinell.Html.Interfaces;

/// <summary>
/// A scope inside another scope, found by its own root element.
/// </summary>
public interface IHtmlContainer<TParent, TSelf> : IHtmlScope<TSelf>
    where TParent : IHtmlScope<TParent>
    where TSelf : IHtmlContainer<TParent, TSelf>
{
    /// <summary>The scope the container is declared in.</summary>
    TParent Parent { get; }

    /// <summary>The container's root element.</summary>
    /// <exception cref="ElementNotFoundException">The root is not there.</exception>
    IHtmlElement ContainerRoot { get; }
}
