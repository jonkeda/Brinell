using Brinell.Maui.Configuration;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// Shared MAUI bottom tab menu control.
/// Uses invokable tab button surfaces when available and avoids pointer-only fallbacks.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public partial class TabMenu<TScope> : Base.ViewBase<TScope>
    where TScope : IMauiScope<TScope>
{
    private const string ButtonId = "TabMenuView_Button";
    private const string CaptionId = "TabMenuView_Caption";
    private const string GridId = "TabMenuView_Grid";

    /// <summary>
    /// Creates a tab menu control within the specified scope.
    /// </summary>
    public TabMenu(IMauiScope<TScope> scope)
        : base(scope, Locator.ByAutomationId("TabMenuView"))
    {
    }

    #region Hand-written Convenience Members

    /// <summary>
    /// Selects a tab by caption.
    /// </summary>
    public TScope Select(string caption, int? timeoutMs = null)
    {
        if (!TrySelect(caption, timeoutMs))
        {
            throw new ElementNotFoundException($"Could not select tab menu item '{caption}'.");
        }

        return ContainingScope;
    }

    /// <summary>
    /// Attempts to select a tab by caption.
    /// Hand-written: selection searches sibling caption/button elements rather than this control's element.
    /// </summary>
    public bool TrySelect(string caption, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caption);

        return RunWait(() => TrySelectNow(caption), timeoutMs);
    }

    private bool TrySelectNow(string caption)
    {
        var captions = MauiScope.FindElements(Locator.ByAutomationId(CaptionId));
        var buttons = MauiScope.FindElements(Locator.ByAutomationId(ButtonId));
        var tabGrids = MauiScope.FindElements(Locator.ByAutomationId(GridId));

        for (var index = 0; index < captions.Count; index++)
        {
            var captionText = captions[index].Text;
            if (!string.Equals(captionText?.Trim(), caption, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index < buttons.Count && TryActivateTabSurface(buttons[index]))
                return true;

            if (index < tabGrids.Count && TryActivateTabSurface(tabGrids[index]))
                return true;

            return TryActivateTabSurface(captions[index]);
        }

        if (TryActivateTabSurface(MauiScope.FindVisibleByName(caption)))
            return true;

        return TryActivateTabSurface(
            MauiScope.FindElements(Locator.ByText(caption)).FirstVisible());
    }

    /// <summary>
    /// Activates one candidate tab surface, reporting failure rather than throwing.
    /// </summary>
    /// <remarks>
    /// A tab renders as several stacked elements — a button, a grid, a caption — and which one
    /// carries the command varies by platform. This walks candidates, so a given one failing
    /// means "not this surface", not a test error, and the caller moves to the next.
    /// A pointer-policy violation still surfaces: that is configuration, not a wrong candidate.
    /// </remarks>
    private static bool TryActivateTabSurface(IMauiElement? element)
    {
        if (!element.HasUsableBounds())
        {
            return false;
        }

        try
        {
            if (element is IInvokePatternElement { SupportsInvokePattern: true } invoke
                && invoke.InvokePattern())
            {
                return true;
            }

            if (element is ILegacyIAccessiblePatternElement { SupportsLegacyIAccessiblePattern: true } legacy
                && legacy.DoDefaultActionPattern())
            {
                return true;
            }

            element!.Click();
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

    #endregion
}
