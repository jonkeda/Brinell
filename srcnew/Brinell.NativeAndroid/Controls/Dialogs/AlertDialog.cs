using Brinell.NativeAndroid.Containers;
using Brinell.NativeAndroid.Controls.Buttons;
using Brinell.NativeAndroid.Controls.Text;

namespace Brinell.NativeAndroid.Controls.Dialogs;

/// <summary>
/// An alert dialog (<c>AlertDialog</c>, <c>MaterialAlertDialogBuilder</c>): its title, message and
/// up to three buttons, by Android's own ids. Shown over the page, so it does not wait for the page.
/// </summary>
/// <typeparam name="TParent">The scope it is declared in, usually the page it opens over.</typeparam>
public class AlertDialog<TParent> : ContainerObjectBase<TParent, AlertDialog<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    // The dialog's frame: the parent of Android's content panel, whatever theme drew it.
    private static readonly Locator Frame = Locator.ByXPath(
        "//*[substring-after(@resource-id, ':id/') = 'parentPanel' or @resource-id = 'android:id/parentPanel']");

    /// <summary>The alert dialog shown over <paramref name="parent"/>.</summary>
    public AlertDialog(INativeAndroidScope<TParent> parent)
        : base(parent, Frame)
    {
    }

    /// <inheritdoc />
    public override AlertDialog<TParent> Self => this;

    /// <summary>Shown over its parent: usable while the page behind it is not.</summary>
    protected override bool AsksParent => false;

    /// <summary>The dialog's title.</summary>
    public TextView<AlertDialog<TParent>> Title => new(this, Locator.ByXPath(".//*[substring-after(@resource-id, ':id/') = 'alertTitle']"));

    /// <summary>The dialog's message.</summary>
    public TextView<AlertDialog<TParent>> Message => new(this, Locator.ByXPath(".//*[@resource-id = 'android:id/message']"));

    /// <summary>The positive button (OK, Save, Allow).</summary>
    public Button<AlertDialog<TParent>> PositiveButton => new(this, Locator.ByXPath(".//*[@resource-id = 'android:id/button1']"));

    /// <summary>The negative button (Cancel).</summary>
    public Button<AlertDialog<TParent>> NegativeButton => new(this, Locator.ByXPath(".//*[@resource-id = 'android:id/button2']"));

    /// <summary>The neutral button (Later, Learn more).</summary>
    public Button<AlertDialog<TParent>> NeutralButton => new(this, Locator.ByXPath(".//*[@resource-id = 'android:id/button3']"));
}
