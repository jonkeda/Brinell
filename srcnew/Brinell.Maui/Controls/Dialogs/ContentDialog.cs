using Brinell.Maui.Configuration;
using Brinell.Maui.Containers;
using Brinell.Maui.Controls.Buttons;
using Brinell.Maui.Controls.Text;

namespace Brinell.Maui.Controls.Dialogs;

/// <summary>
/// MAUI ContentDialog control for WinUI3 popups produced by DisplayAlert and
/// DisplayPromptAsync.
/// </summary>
/// <typeparam name="TParent">The parent scope type.</typeparam>
public class ContentDialog<TParent> : ContainerObjectBase<TParent, ContentDialog<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>
    /// Creates a ContentDialog control in the current scope.
    /// </summary>
    /// <param name="parentScope">The parent scope that owns the dialog interaction.</param>
    public ContentDialog(IMauiScope<TParent> parentScope)
        : base(parentScope, Locator.ByClassName("ContentDialog"))
    {
    }

    /// <inheritdoc />
    protected override IMauiElement FindContainerRootElement()
    {
        return Context.Driver.FindPopupElement(Locator);
    }

    /// <summary>
    /// Finds a dialog button by visible text.
    /// </summary>
    public Button<ContentDialog<TParent>> DialogButton(string buttonText)
        => new(this, Locator.ByName(buttonText));

    /// <summary>
    /// Tries to click a dialog button and waits until the dialog closes.
    /// </summary>
    public bool TryClickButtonAndWaitDismissed(string buttonText, int? timeoutMs = null)
    {
        var timeout = timeoutMs ?? DefaultTimeoutMs;
        var locator = Locator.ByName(buttonText);

        if (TryClickScopedButtonByTextAndWaitDismissed(buttonText, timeout))
            return true;

        if (TryClickScopedDialogButtonAndWaitDismissed(buttonText, timeout))
            return true;

        if (TryClickPopupButtonByControlTypeAndWaitDismissed(buttonText, timeout))
            return true;

        if (TryClickPopupButtonAndWaitDismissed(locator, timeout))
            return true;

        if (TryClickParentButtonByTextAndWaitDismissed(buttonText, timeout))
            return true;

        return TryClickParentButtonAndWaitDismissed(locator, timeout);
    }

    /// <summary>
    /// The text input field inside a DisplayPromptAsync dialog.
    /// </summary>
    public Entry<ContentDialog<TParent>> PromptInput
        => new(this, Locator.ByControlType("entry"));

    private bool TryClickScopedDialogButtonAndWaitDismissed(string buttonText, int timeoutMs)
    {
        DialogButton(buttonText).Click(timeoutMs);

        InvalidateCache();
        return WaitExists(false, timeoutMs);
    }

    private bool TryClickScopedButtonByTextAndWaitDismissed(string buttonText, int timeoutMs)
    {
        var button = FindButtonByText(FindElements(Locator.ByControlType("button")), buttonText);
        if (button == null || !TryActivateDialogButton(button))
            return false;

        InvalidateCache();
        return WaitExists(false, timeoutMs);
    }

    private bool TryClickPopupButtonByControlTypeAndWaitDismissed(string buttonText, int timeoutMs)
    {
        if (!Context.Driver.TryFindPopupElement(Locator.ByControlType("button"), out var button)
            || button == null
            || !MatchesText(button, buttonText))
        {
            return false;
        }

        if (!TryActivateDialogButton(button))
            return false;

        return RunWait(
            () => !Context.Driver.TryFindPopupElement(Locator.ByControlType("button"), out var candidate)
                  || candidate == null
                  || !MatchesText(candidate, buttonText),
            timeoutMs);
    }

    private bool TryClickPopupButtonAndWaitDismissed(Locator locator, int timeoutMs)
    {
        if (!Context.Driver.TryFindPopupElement(locator, out var button) || button == null)
            return false;

        if (!TryActivateDialogButton(button))
            return false;

        return RunWait(
            () => !Context.Driver.TryFindPopupElement(locator, out _),
            timeoutMs);
    }

    private bool TryClickParentButtonByTextAndWaitDismissed(string buttonText, int timeoutMs)
    {
        var locator = Locator.ByControlType("button");
        var button = FindButtonByText(Parent.FindElements(locator), buttonText);
        if (button == null || !TryActivateDialogButton(button))
            return false;

        return RunWait(
            () => FindButtonByText(Parent.FindElements(locator), buttonText) == null,
            timeoutMs);
    }

    private bool TryClickParentButtonAndWaitDismissed(Locator locator, int timeoutMs)
    {
        var button = new Button<TParent>(Parent, locator);
        button.Click(timeoutMs);

        return RunWait(
            () => Parent.TryFindElement(locator) == null,
            timeoutMs);
    }

    /// <summary>
    /// Activates one candidate dialog button, reporting failure rather than throwing.
    /// </summary>
    /// <remarks>
    /// A WinUI3 ContentDialog is reached through several fallbacks — scoped, popup window,
    /// parent scope — and each may hand back a button that turns out not to be the live one.
    /// A failure here means "not this candidate", so the caller tries the next fallback.
    /// A pointer-policy violation still surfaces: that is configuration, not a wrong candidate.
    /// </remarks>
    private static bool TryActivateDialogButton(IMauiElement? button)
    {
        if (!button.HasUsableBounds())
        {
            return false;
        }

        try
        {
            if (button is IInvokePatternElement { SupportsInvokePattern: true } invoke
                && invoke.InvokePattern())
            {
                return true;
            }

            if (button is ILegacyIAccessiblePatternElement { SupportsLegacyIAccessiblePattern: true } legacy
                && legacy.DoDefaultActionPattern())
            {
                return true;
            }

            button!.Click();
            return true;
        }
        catch (WindowsInteractionPolicyException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static IMauiElement? FindButtonByText(IEnumerable<IMauiElement>? buttons, string buttonText)
    {
        if (buttons == null)
            return null;

        return buttons.FirstOrDefault(button => MatchesText(button, buttonText));
    }

    private static bool MatchesText(IMauiElement element, string text)
    {
        return string.Equals(element.Text, text, StringComparison.OrdinalIgnoreCase)
            || string.Equals(element.GetAttribute("name"), text, StringComparison.OrdinalIgnoreCase);
    }
}
