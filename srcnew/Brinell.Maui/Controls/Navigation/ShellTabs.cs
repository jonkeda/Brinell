using Brinell.Maui.Containers;

namespace Brinell.Maui.Controls.Navigation;

/// <summary>
/// A MAUI Shell's tab strip: a collection of <see cref="ShellTab{TParent}"/>.
/// </summary>
/// <remarks>
/// Rooted at the strip the platform draws (see <see cref="ShellChromeLocators"/>), so the tabs are
/// its items and nothing else on the page can be mistaken for one.
/// </remarks>
/// <typeparam name="TParent">The scope the shell belongs to.</typeparam>
public class ShellTabs<TParent> : CollectionObjectBase<TParent, ShellTabs<TParent>, ShellTab<TParent>>
    where TParent : IMauiScope<TParent>
{
    /// <summary>Creates the tab strip within the given scope.</summary>
    /// <param name="scope">The scope the shell belongs to.</param>
    /// <param name="chrome">Where the platform draws Shell, from <see cref="IMauiTestContext.ShellChrome"/>.</param>
    public ShellTabs(IMauiScope<TParent> scope, ShellChromeLocators chrome)
        : base(scope,
               chrome.TabHost,
               ItemStrategy.ByLocator(chrome.Tab),
               (tabs, itemRoot, index) => new ShellTab<TParent>(tabs, itemRoot, index))
    {
    }
}
