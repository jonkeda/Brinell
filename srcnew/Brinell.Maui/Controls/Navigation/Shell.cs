namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// A MAUI Shell: its tab strip and its flyout.
/// </summary>
/// <remarks>
/// <para>
/// Not a control or a container: Shell has no element of its own on any platform. The tab strip
/// and the flyout are each a collection rooted at what the platform draws.
/// </para>
/// <code>
/// Shell.Tabs["Controls"].Click();
/// Shell.Tabs["Controls"].AssertSelected();
/// Shell.Tabs.AssertItemCount(4);
/// Shell.Flyout.Open();
/// Shell.Flyout["Settings"].Click();
/// </code>
/// </remarks>
/// <typeparam name="TParent">The scope the shell belongs to.</typeparam>
public class Shell<TParent>
    where TParent : IMauiScope<TParent>
{
    private readonly Lazy<ShellTabs<TParent>> _tabs;
    private readonly Lazy<ShellFlyout<TParent>> _flyout;

    /// <summary>
    /// Creates a Shell over the given scope.
    /// </summary>
    /// <remarks>
    /// The collections are built on first use, so a platform whose Shell chrome is not mapped
    /// fails only in the member that needs it.
    /// </remarks>
    /// <param name="scope">The scope (normally the page object for the shell's app).</param>
    public Shell(IMauiScope<TParent> scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        _tabs = new Lazy<ShellTabs<TParent>>(
            () => new ShellTabs<TParent>(scope, scope.Context.ShellChrome));
        _flyout = new Lazy<ShellFlyout<TParent>>(
            () => new ShellFlyout<TParent>(scope, scope.Context.ShellChrome));
    }

    /// <summary>The tabs of the current shell item.</summary>
    public ShellTabs<TParent> Tabs => _tabs.Value;

    /// <summary>The flyout.</summary>
    public ShellFlyout<TParent> Flyout => _flyout.Value;
}
