using Bruinell.NativeAndroid.Containers;
using Bruinell.NativeAndroid.Controls.Buttons;
using Bruinell.NativeAndroid.Controls.Text;

namespace Bruinell.NativeAndroid.Controls.Dialogs;

/// <summary>
/// A Material snackbar: its text and its action. It leaves by itself, so assert it soon after the
/// action that shows it.
/// </summary>
/// <typeparam name="TParent">The scope it is declared in, usually the page.</typeparam>
public class Snackbar<TParent> : ContainerObjectBase<TParent, Snackbar<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    // Material's content layout: the snackbar's text and action are its children.
    private static readonly Locator Frame = Locator.ByXPath(
        "//*[substring-after(@resource-id, ':id/') = 'snackbar_text']/..");

    /// <summary>The snackbar shown over <paramref name="parent"/>.</summary>
    public Snackbar(INativeAndroidScope<TParent> parent)
        : base(parent, Frame)
    {
    }

    /// <inheritdoc />
    public override Snackbar<TParent> Self => this;

    /// <summary>
    /// Shown over its page, and gone in seconds: it does not wait for the page, which costs a
    /// probe per attempt the snackbar may not have (a run lost its Undo to that, 2026-10-07).
    /// </summary>
    protected override bool AsksParent => false;

    /// <summary>The snackbar's text.</summary>
    public TextView<Snackbar<TParent>> Text => new(this, Locator.ByXPath(".//*[substring-after(@resource-id, ':id/') = 'snackbar_text']"));

    /// <summary>The snackbar's action (Undo, Retry).</summary>
    public Button<Snackbar<TParent>> Action => new(this, Locator.ByXPath(".//*[substring-after(@resource-id, ':id/') = 'snackbar_action']"));
}
