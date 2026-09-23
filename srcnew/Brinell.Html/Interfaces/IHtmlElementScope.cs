using Brinell.Core.Interfaces;

namespace Brinell.Html.Interfaces;

public interface IHtmlElementScope : IElementScope<IHtmlElement>
{
    IHtmlTestContext Context { get; }
}