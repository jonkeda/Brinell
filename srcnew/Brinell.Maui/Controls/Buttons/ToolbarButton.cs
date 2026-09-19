namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// A MAUI <c>ToolbarItem</c>: a button drawn into the window's chrome rather than into a page.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
/// <remarks>
/// <para>
/// The toolbar belongs to the window, so a toolbar item scoped to a page object cannot be
/// resolved when that page is not showing. Declare it on <c>AppRoot</c>.
/// </para>
/// <para>
/// Clicking raises the item by its id rather than through the Invoke pattern, which on Windows
/// reports success without raising the item's command.
/// </para>
/// </remarks>
public class ToolbarButton<TScope> : Button<TScope>
    where TScope : IMauiScope<TScope>
{
    /// <summary>Creates a toolbar button within the specified scope.</summary>
    /// <param name="scope">The scope, normally <c>AppRoot</c>.</param>
    /// <param name="locator">The locator for the toolbar item.</param>
    public ToolbarButton(IMauiScope<TScope> scope, Locator locator)
        : base(scope, locator)
    {
    }

    /// <summary>Creates a toolbar button using the scope's default locator strategy.</summary>
    /// <remarks>
    /// Prefer the <see cref="Locator"/> constructor with <c>Locator.ByAccessibilityId</c>. MAUI
    /// publishes a <c>ToolbarItem</c>'s <c>AutomationId</c> as the accessibility label, not the
    /// automation id, so the default strategy may not find it on Android.
    /// </remarks>
    /// <param name="scope">The scope, normally <c>AppRoot</c>.</param>
    /// <param name="locatorValue">The locator value.</param>
    public ToolbarButton(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// False: the click raises the item by its id through the bridge, so which instance of the
    /// element is on screen does not matter - only that it is enabled.
    /// </para>
    /// <para>
    /// And waiting on one instance is harmful. On Windows MAUI replaces a toolbar item's element
    /// whenever its command's <c>CanExecute</c> changes (measured 2026-09-18 on the Todo sample: the
    /// runtime id went <c>…4.17</c>, <c>…4.30</c> disabled, missing, <c>…4.65</c> enabled within
    /// 210 ms of a command finishing). A visibility wait that found the outgoing element spent its
    /// whole five seconds on it and failed as "not visible", about one full Todo run in two.
    /// </para>
    /// </remarks>
    protected override bool RequiresVisibilityForAction => false;

    /// <inheritdoc />
    /// <remarks>
    /// Raises the item through the element's <c>InvokeToolbarItem</c>, using the id from the
    /// locator.
    /// </remarks>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.InvokeToolbarItem(Locator.Value);
    }
}
