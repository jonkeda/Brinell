using Brinell.Core.Interfaces;

namespace Brinell.Html.Interfaces;

public interface IHtmlPage<TSelf> : IHtmlScope<TSelf>, IPageObject<IHtmlElement>
    where TSelf : IHtmlPage<TSelf>
{
}