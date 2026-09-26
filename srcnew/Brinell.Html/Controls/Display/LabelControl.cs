using Brinell.Core.Exceptions;
using Brinell.Core.Locators;
using Brinell.Html.Interfaces;

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
    /// Whether the text contains <paramref name="substring"/> now; with a timeout, waits for it.
    /// </summary>
    public bool IsTextContaining(string substring, int? timeoutMs = null)
    {
        return timeoutMs is null or 0
            ? TryFindElement()?.Text?.Contains(substring, StringComparison.OrdinalIgnoreCase) == true
            : RunWaitWithElement(element => Contains(element, substring), timeoutMs);
    }

    /// <summary>
    /// Wait until text contains the specified substring.
    /// </summary>
    public TScope WaitTextContaining(string substring, int? timeoutMs = null)
    {
        if (!RunWaitWithElement(element => Contains(element, substring), timeoutMs))
        {
            throw new TimeoutException($"Text did not contain '{substring}' within timeout");
        }

        return ContainingScope;
    }

    public Task<TScope> WaitTextContainingAsync(string substring, int? timeoutMs = null)
        => Task.FromResult(WaitTextContaining(substring, timeoutMs));

    /// <summary>
    /// Assert text contains the specified substring, waiting for it (R7).
    /// </summary>
    public TScope AssertTextContaining(string substring, int? timeoutMs = null)
    {
        return RunAssert(element =>
        {
            var text = GetTextCore(element);
            if (text?.Contains(substring, StringComparison.OrdinalIgnoreCase) != true)
            {
                throw new AssertionException(
                    $"Text does not contain '{substring}'. Actual: '{text}'");
            }
        }, timeoutMs);
    }

    public Task<TScope> AssertTextContainingAsync(string substring)
        => Task.FromResult(AssertTextContaining(substring));

    private bool Contains(IHtmlElement element, string substring)
        => GetTextCore(element)?.Contains(substring, StringComparison.OrdinalIgnoreCase) == true;
}
