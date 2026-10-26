namespace Bruinet.Html.Interfaces;

public interface IHtmlScope<TScope> : IHtmlElementScope
    where TScope : IHtmlScope<TScope>
{
    TScope Self { get; }
}
