using Brinell.Maui.Enums;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// A MAUI Shell: its tab strip and its flyout.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately not a control and not a container. Shell has no element of its own that any
/// platform exposes - the tree holds panes and hosts the app never named - so an object
/// rooted at "the Shell" would be rooted at nothing. What does exist is the tab strip and the
/// flyout, and each of those is a collection rooted at what the platform draws.
/// </para>
/// <code>
/// Shell.Tabs["Controls"].Click();
/// Shell.Tabs["Controls"].AssertSelected();
/// Shell.Tabs.AssertItemCount(4);
/// Shell.Flyout.Open();
/// Shell.Flyout["Settings"].Click();
/// </code>
/// <para>
/// This replaces <c>NavigateTo(title)</c>, <c>GetTab(title)</c>, <c>IsTabSelected(title)</c>,
/// <c>WaitTabSelected(...)</c> and <c>AssertTabSelected(...)</c> - five container methods that
/// existed only because a tab was not an object - along with a <c>GetSelectedTab</c> that
/// always returned null and an <c>IsLoaded</c> that always returned true.
/// </para>
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
    /// The two collections are built on first use rather than here, because each asks
    /// <see cref="ShellChrome"/> where its platform draws things and that refuses for a
    /// platform nobody has mapped yet. Building them eagerly would make an unmapped platform
    /// fail while a fixture was being constructed - before any test, and nowhere near the
    /// member that actually needs the answer.
    /// </remarks>
    /// <param name="scope">The scope (normally the page object for the shell's app).</param>
    public Shell(IMauiScope<TParent> scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        _tabs = new Lazy<ShellTabs<TParent>>(
            () => new ShellTabs<TParent>(scope, scope.Context.Driver.Platform));
        _flyout = new Lazy<ShellFlyout<TParent>>(
            () => new ShellFlyout<TParent>(scope, scope.Context.Driver.Platform));
    }

    /// <summary>The tabs of the current shell item.</summary>
    public ShellTabs<TParent> Tabs => _tabs.Value;

    /// <summary>The flyout.</summary>
    public ShellFlyout<TParent> Flyout => _flyout.Value;
}
