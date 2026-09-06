using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// A MAUI Shell's flyout: a collection of <see cref="ShellFlyoutItem{TParent}"/>, with the
/// affordance that reveals them.
/// </summary>
/// <remarks>
/// A flyout is open when its host is visible <b>and</b> holds items. Neither half is enough on
/// its own: Windows creates its pane on first opening and then keeps it hidden with its items
/// still in the tree, so items alone would read as open forever; Android's host is the app's
/// content frame, always visible, and it is the items that come and go. Each half was measured
/// on the platform that needs it.
/// <para>
/// Asking whether the <i>items</i> are visible looks like the tidier rule and is not: Windows
/// hosts an open pane in a window of its own, where its items report themselves off-screen.
/// </para>
/// </remarks>
/// <typeparam name="TParent">The scope the shell belongs to.</typeparam>
public partial class ShellFlyout<TParent>
    : CollectionObjectBase<TParent, ShellFlyout<TParent>, ShellFlyoutItem<TParent>>
    where TParent : IMauiScope<TParent>
{
    private readonly IMauiScope<TParent> _scope;
    private readonly MauiPlatform _platform;

    /// <summary>Creates the flyout within the given scope.</summary>
    public ShellFlyout(IMauiScope<TParent> scope, MauiPlatform platform)
        : base(scope,
               ShellChrome.FlyoutHost(platform),
               ItemStrategy.ByLocator(ShellChrome.FlyoutItem(platform)),
               (flyout, itemRoot, index) => new ShellFlyoutItem<TParent>(flyout, itemRoot, index))
    {
        _scope = scope;
        _platform = platform;
    }

    /// <summary>
    /// The flyout's root is resolved afresh every time.
    /// </summary>
    /// <remarks>
    /// The platform creates and destroys this pane as it opens and closes, so a cached root
    /// outlives the thing it points at. A dead UI Automation element does not always announce
    /// itself either: it can keep answering for its type while reporting no children at all,
    /// which reads as a flyout that opened and stayed empty.
    /// </remarks>
    protected override bool CacheContainerRoot => false;

    /// <summary>
    /// Opens the flyout, and does nothing when it is already open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hand-written rather than generated from an <c>OpenCore</c>: the opener is chrome outside
    /// the flyout, and the flyout's own root does not exist yet at the moment it is needed, so
    /// there is no element for a generated wrapper to resolve and hand over.
    /// </para>
    /// <para>
    /// Idempotent, unlike <c>Menu.Open</c>: a menu's trigger is an app button whose toggling is
    /// the app's own behaviour, while this opener is drawn by the platform and clicking it on an
    /// open flyout is not defined.
    /// </para>
    /// </remarks>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The flyout, for chaining.</returns>
    public ShellFlyout<TParent> Open(int? timeoutMs = null)
    {
        if (IsOpen() == true) return Self;

        Activate(_scope.FindElement(ShellChrome.FlyoutOpener(_platform)));
        InvalidateCache();
        WaitOpen(true, timeoutMs);

        return Self;
    }

    /// <summary>
    /// Dismisses the flyout without choosing anything in it, and does nothing when it is
    /// already shut.
    /// </summary>
    /// <remarks>
    /// A fixture needs this: a test that opens the flyout and asserts on it leaves an overlay
    /// covering the tabs, and the next test would click into it.
    /// </remarks>
    /// <param name="timeoutMs">Optional timeout.</param>
    /// <returns>The flyout, for chaining.</returns>
    public ShellFlyout<TParent> Close(int? timeoutMs = null)
    {
        if (IsOpen() != true) return Self;

        ShellChrome.DismissFlyout(Context, _platform);
        InvalidateCache();
        WaitOpen(false, timeoutMs);

        return Self;
    }

    /// <summary>
    /// Presses a piece of chrome through its automation pattern, falling back to a click.
    /// </summary>
    /// <remarks>
    /// The flyout's opener sits in the window's title-bar strip on Windows, where a synthetic
    /// pointer click is intercepted before it reaches the button and the flyout simply never
    /// opens. Asking the element to invoke itself needs no coordinates. This is the same ladder
    /// every control click walks, applied to chrome the app did not draw.
    /// </remarks>
    private static void Activate(IMauiElement element)
    {
        if (ActivationHelper.TryActivateByPattern(element)) return;

        element.Click();
    }

    #region Core Methods (Element-Aware, No Logging)

    /// <summary>
    /// Whether the flyout is showing its items.
    /// </summary>
    /// <param name="element">The flyout's item host (may be null).</param>
    [AbsenceTolerant]
    protected virtual bool? IsOpenCore(IMauiElement? element)
    {
        if (element == null) return false;

        return element.Visible && TryGetItemRoots().Count > 0;
    }

    #endregion
}
