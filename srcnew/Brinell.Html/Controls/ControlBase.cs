using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

namespace Brinell.Html.Controls;

public abstract class ControlBase<TScope> : ObjectBase, IControlObject<TScope>
    where TScope : IHtmlScope<TScope>
{
    private readonly IHtmlScope<TScope> _scope;

    protected ControlBase(IHtmlScope<TScope> scope, Locator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    protected ControlBase(IHtmlScope<TScope> scope, string selectorOrId)
        : this(scope, ResolveLocator(selectorOrId))
    {
    }

    protected Locator Locator { get; }

    protected TScope ContainingScope => _scope.Self;

    public override IHtmlTestContext Context => _scope.Context;

    protected IHtmlElement? TryFindElement() => _scope.TryFindElement(Locator);

    protected IHtmlElement FindElement() => _scope.FindElement(Locator);

    protected TScope RunWithElement(Action<IHtmlElement> action)
    {
        var element = FindElement();
        action(element);
        return ContainingScope;
    }

    protected TResult RunWithElement<TResult>(Func<IHtmlElement, TResult> action)
    {
        var element = FindElement();
        return action(element);
    }

    protected TScope RunAssert(Action<IHtmlElement> assertion)
    {
        var element = FindElement();
        assertion(element);
        return ContainingScope;
    }

    public bool IsExists()
    {
        return TryFindElement() != null;
    }

    public bool? IsVisible()
    {
        return TryFindElement()?.Visible;
    }

    public bool? IsEnabled()
    {
        return TryFindElement()?.Enabled;
    }

    public bool WaitExists(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return Poll(() => IsExists() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public bool WaitVisible(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return Poll(() => IsVisible() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public bool WaitEnabled(bool? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return Poll(() => IsEnabled() == expected.Value, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertExists(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        if (!WaitExists(expected, timeoutMs))
        {
            var actual = IsExists();
            throw new AssertionException(message ??
                $"Expected element {(expected.Value ? "to exist" : "not to exist")} but it {(actual ? "exists" : "does not exist")}. Locator: {Locator}");
        }

        return ContainingScope;
    }

    public TScope AssertVisible(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        if (!WaitVisible(expected, timeoutMs))
        {
            var actual = IsVisible();
            throw new AssertionException(message ??
                $"Expected element {(expected.Value ? "to be visible" : "not to be visible")} but visibility is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }

        return ContainingScope;
    }

    public TScope AssertEnabled(bool? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        if (!WaitEnabled(expected, timeoutMs))
        {
            var actual = IsEnabled();
            throw new AssertionException(message ??
                $"Expected element {(expected.Value ? "to be enabled" : "to be disabled")} but enabled state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }

        return ContainingScope;
    }

    public string? GetText(int? timeoutMs = null)
    {
        if (timeoutMs.HasValue)
        {
            WaitExists(true, timeoutMs.Value);
        }

        return TryFindElement()?.Text;
    }

    public bool WaitText(string? expected, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return true;
        }

        return Poll(() => GetText() == expected, timeoutMs ?? DefaultTimeoutMs);
    }

    public TScope AssertText(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        if (!WaitText(expected, timeoutMs))
        {
            var actual = GetText();
            throw new AssertionException(message ??
                $"Expected text '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }

        return ContainingScope;
    }

    public TScope AssertTextContains(string? expected, string? message = null, int? timeoutMs = null)
    {
        if (expected == null)
        {
            return ContainingScope;
        }

        var passed = Poll(() => GetText()?.Contains(expected) == true, timeoutMs ?? DefaultTimeoutMs);

        if (!passed)
        {
            var actual = GetText();
            throw new AssertionException(message ??
                $"Expected text to contain '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }

        return ContainingScope;
    }

    public string? GetAttribute(string name)
    {
        var element = TryFindElement();
        if (element == null)
        {
            return null;
        }

        return element.GetAttribute(name);
    }

    protected static Locator ResolveLocator(string selectorOrId)
    {
        if (string.IsNullOrWhiteSpace(selectorOrId))
        {
            throw new ArgumentNullException(nameof(selectorOrId));
        }

        return IsRawSelector(selectorOrId)
            ? Locator.ByCss(selectorOrId)
            : Locator.ByAutomationId(selectorOrId);
    }

    private static bool IsRawSelector(string value)
    {
        return value.StartsWith('#') ||
               value.StartsWith('.') ||
               value.StartsWith('[') ||
               value.Contains('#') ||
               value.Contains('[') ||
               value.Contains(':') ||
               value.Contains('>') ||
               value.Contains(' ');
    }
}