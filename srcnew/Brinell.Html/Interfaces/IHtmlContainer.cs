using Brinell.Core.Interfaces;

namespace Brinell.Html.Interfaces;

public interface IHtmlContainer<TParent, TSelf> : IHtmlScope<TSelf>, IContainerControl<IHtmlElement>
    where TParent : IHtmlScope<TParent>
    where TSelf : IHtmlContainer<TParent, TSelf>
{
    TParent Parent { get; }
}