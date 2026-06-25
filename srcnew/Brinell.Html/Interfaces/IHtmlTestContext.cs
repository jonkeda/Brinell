using Brinell.Core.Interfaces;

namespace Brinell.Html.Interfaces;

public interface IHtmlTestContext : ITestContext<IHtmlElement>, IHtmlElementScope
{
    new IHtmlTestContext Context { get; }
    string CurrentUrl { get; }
    string PageTitle { get; }
    void GoForward();
    bool IsIdle();
}
