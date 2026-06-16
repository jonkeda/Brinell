namespace Brinell.Maui.Extensions.Controls.Navigation;

/// <summary>
/// Shared MAUI bottom tab menu control.
/// Uses invokable tab button surfaces when available and avoids pointer-only fallbacks.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
public class TabMenu<TScope> : ControlBase<TScope>
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
    /// </summary>
    public bool TrySelect(string caption, int? timeoutMs = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caption);

        return Run(nameof(TrySelect), caption, () =>
        {
            var timeout = TimeSpan.FromMilliseconds(timeoutMs ?? DefaultTimeoutMs);
            return ElementSearch.WaitUntil(() => TrySelectNow(caption), timeout);
        });
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

            if (index < buttons.Count && ElementActivator.TryActivate(buttons[index]))
                return true;

            if (index < tabGrids.Count && ElementActivator.TryActivate(tabGrids[index]))
                return true;

            return ElementActivator.TryActivate(captions[index]);
        }

        if (ElementActivator.TryActivate(ElementSearch.FindVisibleByName(MauiScope, caption)))
            return true;

        return ElementActivator.TryActivate(
            ElementSearch.FirstVisible(MauiScope.FindElements(Locator.ByText(caption))));
    }
}
