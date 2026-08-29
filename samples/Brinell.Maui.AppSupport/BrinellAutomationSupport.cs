#if WINDOWS
using Brinell.Maui.AppSupport.Handlers;
#endif
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;

namespace Brinell.Maui.AppSupport;

/// <summary>
/// Registers the Windows automation handlers that make MAUI layout and content
/// containers addressable by <c>AutomationId</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is needed.</b> Stock MAUI layouts (<c>Grid</c>, the stack layouts,
/// <c>FlexLayout</c>, <c>AbsoluteLayout</c>) and content containers
/// (<c>ContentView</c>, <c>Border</c>) map to WinUI panels that have no
/// AutomationPeer. Their <c>AutomationId</c> is therefore invisible to UI Automation
/// — FlaUI, WinAppDriver, and Appium cannot see them at all. Any Brinell container
/// object targeting such a layout will fail to resolve, and the only symptom is an
/// <c>ElementNotFoundException</c> that looks exactly like a mistyped AutomationId.
/// </para>
/// <para>
/// <b>Two supported ways to use this.</b> Reference this project and call
/// <see cref="AddBrinellAutomationHandlers"/>, or copy the <c>Handlers</c> folder and
/// this file directly into the app under test. Both are expected: the app under test
/// is not always one you can add a project reference to.
/// </para>
/// <example>
/// <code>
/// builder.ConfigureMauiHandlers(handlers =&gt; handlers.AddBrinellAutomationHandlers());
/// </code>
/// </example>
/// </remarks>
public static class BrinellAutomationSupport
{
    /// <summary>
    /// Registers automation handlers for <c>Layout</c>, <c>ContentView</c>, and
    /// <c>Border</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>Layout</c> registration is against the base type, so it covers every
    /// layout subclass at once.
    /// </para>
    /// <para>
    /// <b>SwipeView and RefreshView are deliberately absent.</b> They map to the WinUI
    /// <c>SwipeControl</c> and <c>RefreshContainer</c>, which already supply their own
    /// AutomationPeers. Overriding those peers does not just fail to help — it
    /// collapses the entire UIA tree, making every element in the app unaddressable
    /// while the app continues to render normally. This was measured, not assumed. To
    /// scope inside a SwipeView or RefreshView, wrap its <i>content</i> in a container
    /// that is addressable.
    /// </para>
    /// <para>
    /// <c>Frame</c> is also absent: it is deprecated in MAUI and has no handler to
    /// hook. Use <c>Border</c>, which is supported here.
    /// </para>
    /// </remarks>
    /// <param name="handlers">The handler collection from <c>ConfigureMauiHandlers</c>.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IMauiHandlersCollection AddBrinellAutomationHandlers(
        this IMauiHandlersCollection handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

#if WINDOWS
        // One registration against the base Layout type covers Grid, the stack
        // layouts, FlexLayout, and AbsoluteLayout.
        handlers.AddHandler<Layout, AutomationLayoutHandler>();
        handlers.AddHandler<ContentView, AutomationContentViewHandler>();
        handlers.AddHandler<Border, AutomationBorderHandler>();
#endif

        return handlers;
    }
}
