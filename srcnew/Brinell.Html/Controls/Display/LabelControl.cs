using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;
using Brinell.Html.Interfaces.Async;

namespace Brinell.Html.Controls.Display;

/// <summary>
/// HTML label/text display control. Wraps any element used for text display
/// (&lt;label&gt;, &lt;span&gt;, &lt;p&gt;, &lt;h1-h6&gt;, etc.).
/// </summary>
public class LabelControl<TScope> : ControlBase<TScope>
    where TScope : IHtmlScope<TScope>
{
    public LabelControl(IHtmlScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    public LabelControl(IHtmlScope<TScope> scope, string selectorOrId)
        : base(scope, selectorOrId)
    {
    }

    /// <summary>
    /// Check if text contains the specified substring.
    /// </summary>
    public bool IsTextContaining(string substring, int? timeoutMs = null)
    {
        return Poll(
            () => TryFindElement()?.Text?.Contains(substring, StringComparison.OrdinalIgnoreCase) == true,
            timeoutMs ?? 0);
    }

    /// <summary>
    /// Wait until text contains the specified substring.
    /// </summary>
    public TScope WaitTextContaining(string substring, int? timeoutMs = null)
    {
        if (!IsTextContaining(substring, timeoutMs ?? DefaultTimeoutMs))
        {
            throw new TimeoutException($"Text did not contain '{substring}' within timeout");
        }

        return ContainingScope;
    }

    /// <summary>
    /// Assert text contains the specified substring.
    /// </summary>
    public TScope AssertTextContaining(string substring)
    {
        var text = GetText();
        if (text?.Contains(substring, StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new AssertionException(
                $"Text does not contain '{substring}'. Actual: '{text}'");
        }

        return ContainingScope;
    }

    public async Task<bool> IsTextContainingAsync(string substring, int? timeoutMs = null)
    {
        return await PollAsync(async () =>
        {
            var element = TryFindAsyncElement();
            if (element == null) return false;
            var text = await element.GetText().ConfigureAwait(false);
            return text?.Contains(substring, StringComparison.OrdinalIgnoreCase) == true;
        }, timeoutMs ?? 0).ConfigureAwait(false);
    }

    public async Task<TScope> WaitTextContainingAsync(string substring, int? timeoutMs = null)
    {
        if (!await IsTextContainingAsync(substring, timeoutMs ?? DefaultTimeoutMs).ConfigureAwait(false))
        {
            throw new TimeoutException($"Text did not contain '{substring}' within timeout");
        }
        return ContainingScope;
    }

    public async Task<TScope> AssertTextContainingAsync(string substring)
    {
        var self = (IHtmlAsyncControlObject<TScope>)this;
        var text = await self.GetText().ConfigureAwait(false);
        if (text?.Contains(substring, StringComparison.OrdinalIgnoreCase) != true)
        {
            throw new AssertionException(
                $"Text does not contain '{substring}'. Actual: '{text}'");
        }
        return ContainingScope;
    }
}
