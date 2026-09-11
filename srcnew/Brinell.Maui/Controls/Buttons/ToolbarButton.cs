namespace Brinell.Maui.Controls.Buttons;

/// <summary>
/// A MAUI <c>ToolbarItem</c>: a button drawn into the window's chrome rather than into a page.
/// </summary>
/// <typeparam name="TScope">The containing scope type for fluent chaining.</typeparam>
/// <remarks>
/// <para>
/// <b>Two things differ from an ordinary <see cref="Button{TScope}"/>, and both come from where
/// it is drawn.</b>
/// </para>
/// <para>
/// <b>It is not inside a page.</b> The toolbar belongs to the window, so a toolbar item scoped to
/// a page object cannot be resolved when that page is not showing - which for a back affordance
/// is exactly when it is wanted. Declare it on <c>AppRoot</c>.
/// </para>
/// <para>
/// <b>Its automation peer accepts an Invoke that goes nowhere.</b> This is the important one, and
/// it is measured rather than assumed. On Windows, driving a MAUI <c>ToolbarItem</c> through the
/// Invoke pattern <i>reports success</i> and does not raise the command:
/// </para>
/// <list type="table">
/// <item><description>Invoke alone - 4 failures in 4.</description></item>
/// <item><description>Invoke, then click if it returned false - 3 failures in 4. The fallback
/// never fires, because Invoke said it worked.</description></item>
/// <item><description>The shared activation ladder, which worked for every other
/// control in the suite - 1 failure in 3, and the run went from 5 s to 58 s.</description></item>
/// <item><description>A plain click - stable.</description></item>
/// </list>
/// <para>
/// So this control declines the pattern ladder outright instead of relying on falling through it.
/// A silent no-op is worse than an honest click: it turns navigation that did not happen into a
/// timeout somewhere else. The full account is in
/// <c>.my/extension/physical-input-inventory.md</c>.
/// </para>
/// <para>
/// <b>Prefer a semantic route where the app has one.</b> A click is real pointer input and is
/// refused in background mode. Where the app under test publishes the Brinell automation bridge,
/// ask it to navigate instead - <c>IMauiDriver.TryNavigateBack</c> - and keep this for the
/// platforms and apps that have no bridge.
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
    /// <b>Usually the wrong constructor for this control.</b> MAUI surfaces a
    /// <c>ToolbarItem</c>'s <c>AutomationId</c> as the platform's accessibility label, not as its
    /// automation id: on Android the node's <c>resource-id</c> is empty and the value appears in
    /// <c>content-desc</c>. <c>Locator.ByAccessibilityId</c> is the same string on all three
    /// platforms, so prefer the other constructor.
    /// </remarks>
    /// <param name="scope">The scope, normally <c>AppRoot</c>.</param>
    /// <param name="locatorValue">The locator value.</param>
    public ToolbarButton(IMauiScope<TScope> scope, string locatorValue)
        : base(scope, locatorValue)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// A real pointer click, named outright. See the type remarks: the Invoke pattern reports
    /// success and raises nothing on this control, so asking for it would be a way of never
    /// clicking.
    /// </remarks>
    protected override void ClickCore(IMauiElement element, int? timeoutMs = null)
    {
        EnsureClickableCore(element);
        element.Click();
    }
}
