using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls.Text;

/// <summary>
/// HTML text input control. Wraps &lt;input type="text|email|password|search|tel|url"&gt;.
/// </summary>
public class TextInputControl<TScope> : FocusableControlBase<TScope>, IHtmlAsyncEditable<TScope>
    where TScope : IHtmlScope<TScope>
{
    public TextInputControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public TextInputControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>
    /// Clear and fill with new text (uses Playwright Fill for reliable input).
    /// </summary>
    public TScope SetText(string text)
    {
        return RunWithElement(element => element.Fill(text));
    }

    /// <summary>
    /// Get the current input value.
    /// </summary>
    public string GetValue()
    {
        return RunWithElement(element => element.InputValue);
    }

    /// <summary>
    /// Type text character by character (for inputs with keystroke handlers).
    /// </summary>
    public TScope TypeText(string text)
    {
        return RunWithElement(element => element.SendKeys(text));
    }

    /// <summary>
    /// Assert current input value matches expected.
    /// </summary>
    public TScope AssertValue(string? expected)
    {
        return RunAssert(element =>
        {
            var actual = element.InputValue;
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new AssertionException(
                    $"Input value mismatch. Expected: '{expected}', Actual: '{actual}'");
            }
        });
    }

    /// <summary>
    /// Wait until input value equals expected.
    /// </summary>
    public TScope WaitValue(string? expected, int? timeoutMs = null)
    {
        var matched = Poll(() =>
        {
            var element = TryFindElement();
            return element != null && string.Equals(element.InputValue, expected, StringComparison.Ordinal);
        }, timeoutMs ?? DefaultTimeoutMs);

        if (!matched)
        {
            throw new TimeoutException($"Input value did not match '{expected}' within timeout");
        }

        return ContainingScope;
    }

    #region IHtmlAsyncEditable<TScope> explicit implementation

    async Task<TScope> IHtmlAsyncEditable<TScope>.SetText(string text)
        => await RunWithElementAsync(async e =>
        {
            await e.Clear().ConfigureAwait(false);
            await e.Fill(text).ConfigureAwait(false);
        }).ConfigureAwait(false);

    async Task<string> IHtmlAsyncEditable<TScope>.GetValue()
        => await RunWithElementAsync<string>(async e =>
            await e.GetInputValue().ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncEditable<TScope>.TypeText(string text)
        => await RunWithElementAsync(async e =>
            await e.SendKeys(text).ConfigureAwait(false)).ConfigureAwait(false);

    async Task<TScope> IHtmlAsyncEditable<TScope>.AssertValue(string? expected)
    {
        if (expected == null) return ContainingScope;
        var self = (IHtmlAsyncEditable<TScope>)this;
        var timeout = DefaultTimeoutMs;
        var matched = await PollAsync(async () =>
        {
            var value = await self.GetValue().ConfigureAwait(false);
            return string.Equals(value, expected, StringComparison.Ordinal);
        }, timeout).ConfigureAwait(false);

        if (!matched)
        {
            var actual = await self.GetValue().ConfigureAwait(false);
            throw new AssertionException(
                $"Input value mismatch. Expected: '{expected}', Actual: '{actual}'");
        }
        return ContainingScope;
    }

    async Task<TScope> IHtmlAsyncEditable<TScope>.WaitValue(string? expected, int? timeoutMs)
    {
        if (expected == null) return ContainingScope;
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var self = (IHtmlAsyncEditable<TScope>)this;
        var matched = await PollAsync(async () =>
        {
            var value = await self.GetValue().ConfigureAwait(false);
            return string.Equals(value, expected, StringComparison.Ordinal);
        }, timeout).ConfigureAwait(false);

        if (!matched)
        {
            throw new TimeoutException($"Input value did not match '{expected}' within timeout");
        }
        return ContainingScope;
    }

    async Task<TScope> IHtmlAsyncEditable<TScope>.AppendText(string text)
        => await RunWithElementAsync(async e =>
        {
            await e.Focus().ConfigureAwait(false);
            await e.SendKeys(text).ConfigureAwait(false);
        }).ConfigureAwait(false);

    #endregion
}
