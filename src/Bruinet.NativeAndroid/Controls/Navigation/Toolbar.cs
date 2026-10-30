using Bruinet.NativeAndroid.Containers;
using Bruinet.NativeAndroid.Controls.Buttons;
using Bruinet.NativeAndroid.Controls.Display;
using Bruinet.NativeAndroid.Controls.Text;

namespace Bruinet.NativeAndroid.Controls.Navigation;

/// <summary>
/// An app bar (<c>Toolbar</c>, <c>MaterialToolbar</c>): its title, its navigation button (up or
/// the drawer), its action items and its overflow menu.
/// </summary>
/// <typeparam name="TParent">The scope it is declared in.</typeparam>
public class Toolbar<TParent> : ContainerObjectBase<TParent, Toolbar<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    /// <summary>The toolbar found by <paramref name="locator"/>.</summary>
    public Toolbar(INativeAndroidScope<TParent> parent, Locator locator)
        : base(parent, locator)
    {
    }

    /// <summary>The toolbar with resource id <paramref name="resourceId"/>.</summary>
    public Toolbar(INativeAndroidScope<TParent> parent, string resourceId)
        : base(parent, resourceId)
    {
    }

    /// <inheritdoc />
    public override Toolbar<TParent> Self => this;

    /// <summary>The title: the toolbar's first text.</summary>
    public TextView<Toolbar<TParent>> Title => new(this, Locator.ByXPath("./android.widget.TextView[1]"));

    /// <summary>
    /// The navigation button: the toolbar's image button, "Navigate up" or "Open navigation drawer"
    /// as the app describes it.
    /// </summary>
    public ImageButton<Toolbar<TParent>> NavigationButton => new(this, Locator.ByXPath("./android.widget.ImageButton"));

    /// <summary>The overflow menu's button ("More options").</summary>
    public ImageView<Toolbar<TParent>> MoreOptions => new(this, Locator.ByAccessibilityId("More options"));

    /// <summary>An action item, by its title or content description.</summary>
    public Button<Toolbar<TParent>> Action(string title) => new(this, Locator.ByText(title));
}
