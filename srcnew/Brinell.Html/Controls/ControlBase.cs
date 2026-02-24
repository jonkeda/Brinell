using Brinell.Core.Exceptions;
using Brinell.Core.Interfaces;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls;

public abstract class ControlBase<TScope> : ObjectBase,
    IControlObject<TScope>,
    IHtmlAsyncControlObject<TScope>
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

    protected IAsyncHtmlElement? TryFindAsyncElement()
        => TryFindElement() as IAsyncHtmlElement;

    protected IAsyncHtmlElement FindAsyncElement()
        => FindElement() as IAsyncHtmlElement
            ?? throw new InvalidOperationException(
                $"Element for '{Locator}' does not support async operations. " +
                "Ensure the test context uses an async-capable element implementation.");

    protected async Task<TScope> RunWithElementAsync(Func<IAsyncHtmlElement, Task> action)
    {
        var element = FindAsyncElement();
        await action(element).ConfigureAwait(false);
        return ContainingScope;
    }

    protected async Task<TResult> RunWithElementAsync<TResult>(Func<IAsyncHtmlElement, Task<TResult>> action)
    {
        var element = FindAsyncElement();
        return await action(element).ConfigureAwait(false);
    }

    protected async Task<TScope> RunAssertAsync(Func<IAsyncHtmlElement, Task> assertion)
    {
        var element = FindAsyncElement();
        await assertion(element).ConfigureAwait(false);
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

    #region IHtmlAsyncControlObject<TScope> explicit implementation

    async Task<bool> IHtmlAsyncControlObject<TScope>.IsExists()
    {
        var element = TryFindAsyncElement();
        return element != null;
    }

    async Task<bool?> IHtmlAsyncControlObject<TScope>.IsVisible()
    {
        var element = TryFindAsyncElement();
        return element != null ? await element.IsVisible().ConfigureAwait(false) : null;
    }

    async Task<bool?> IHtmlAsyncControlObject<TScope>.IsEnabled()
    {
        var element = TryFindAsyncElement();
        return element != null ? await element.IsEnabled().ConfigureAwait(false) : null;
    }

    async Task<bool> IHtmlAsyncControlObject<TScope>.WaitExists(bool? expected, int? timeoutMs)
    {
        if (expected == null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return await PollAsync(async () =>
        {
            var exists = TryFindAsyncElement() != null;
            return exists == expected.Value;
        }, timeout).ConfigureAwait(false);
    }

    async Task<bool> IHtmlAsyncControlObject<TScope>.WaitVisible(bool? expected, int? timeoutMs)
    {
        if (expected == null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return await PollAsync(async () =>
        {
            var element = TryFindAsyncElement();
            if (element == null) return !expected.Value;
            return await element.IsVisible().ConfigureAwait(false) == expected.Value;
        }, timeout).ConfigureAwait(false);
    }

    async Task<bool> IHtmlAsyncControlObject<TScope>.WaitEnabled(bool? expected, int? timeoutMs)
    {
        if (expected == null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return await PollAsync(async () =>
        {
            var element = TryFindAsyncElement();
            if (element == null) return false;
            return await element.IsEnabled().ConfigureAwait(false) == expected.Value;
        }, timeout).ConfigureAwait(false);
    }

    async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertExists(bool? expected, string? message, int? timeoutMs)
    {
        if (expected == null) return ContainingScope;
        var self = (IHtmlAsyncControlObject<TScope>)this;
        if (!await self.WaitExists(expected, timeoutMs).ConfigureAwait(false))
        {
            var actual = await self.IsExists().ConfigureAwait(false);
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to exist" : "not to exist")} but it {(actual ? "exists" : "does not exist")}. Locator: {Locator}");
        }
        return ContainingScope;
    }

    async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertVisible(bool? expected, string? message, int? timeoutMs)
    {
        if (expected == null) return ContainingScope;
        var self = (IHtmlAsyncControlObject<TScope>)this;
        if (!await self.WaitVisible(expected, timeoutMs).ConfigureAwait(false))
        {
            var actual = await self.IsVisible().ConfigureAwait(false);
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be visible" : "not to be visible")} but visibility is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }
        return ContainingScope;
    }

    async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertEnabled(bool? expected, string? message, int? timeoutMs)
    {
        if (expected == null) return ContainingScope;
        var self = (IHtmlAsyncControlObject<TScope>)this;
        if (!await self.WaitEnabled(expected, timeoutMs).ConfigureAwait(false))
        {
            var actual = await self.IsEnabled().ConfigureAwait(false);
            throw new AssertionException(
                message ?? $"Expected element {(expected.Value ? "to be enabled" : "to be disabled")} but enabled state is {actual?.ToString() ?? "unknown (element not found)"}. Locator: {Locator}");
        }
        return ContainingScope;
    }

    async Task<string?> IHtmlAsyncControlObject<TScope>.GetText(int? timeoutMs)
    {
        if (timeoutMs.HasValue)
        {
            var self = (IHtmlAsyncControlObject<TScope>)this;
            await self.WaitExists(true, timeoutMs.Value).ConfigureAwait(false);
        }
        var element = TryFindAsyncElement();
        return element != null ? await element.GetText().ConfigureAwait(false) : null;
    }

    async Task<bool> IHtmlAsyncControlObject<TScope>.WaitText(string? expected, int? timeoutMs)
    {
        if (expected == null) return true;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        return await PollAsync(async () =>
        {
            var element = TryFindAsyncElement();
            if (element == null) return false;
            var text = await element.GetText().ConfigureAwait(false);
            return text == expected;
        }, timeout).ConfigureAwait(false);
    }

    async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertText(string? expected, string? message, int? timeoutMs)
    {
        if (expected == null) return ContainingScope;
        var self = (IHtmlAsyncControlObject<TScope>)this;
        if (!await self.WaitText(expected, timeoutMs).ConfigureAwait(false))
        {
            var actual = await self.GetText().ConfigureAwait(false);
            throw new AssertionException(
                message ?? $"Expected text '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
        return ContainingScope;
    }

    async Task<TScope> IHtmlAsyncControlObject<TScope>.AssertTextContains(string? expected, string? message, int? timeoutMs)
    {
        if (expected == null) return ContainingScope;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var matched = await PollAsync(async () =>
        {
            var element = TryFindAsyncElement();
            if (element == null) return false;
            var text = await element.GetText().ConfigureAwait(false);
            return text?.Contains(expected, StringComparison.Ordinal) == true;
        }, timeout).ConfigureAwait(false);

        if (!matched)
        {
            var self = (IHtmlAsyncControlObject<TScope>)this;
            var actual = await self.GetText().ConfigureAwait(false);
            throw new AssertionException(
                message ?? $"Expected text to contain '{expected}' but got '{actual ?? "(null)"}'. Locator: {Locator}");
        }
        return ContainingScope;
    }

    async Task<string?> IHtmlAsyncControlObject<TScope>.GetAttribute(string name)
    {
        return await RunWithElementAsync<string?>(async e =>
            await e.GetAttribute(name).ConfigureAwait(false)).ConfigureAwait(false);
    }

    #endregion

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