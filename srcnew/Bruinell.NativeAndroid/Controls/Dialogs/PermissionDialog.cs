using Bruinell.NativeAndroid.Containers;
using Bruinell.NativeAndroid.Controls.Buttons;
using Bruinell.NativeAndroid.Controls.Text;

namespace Bruinell.NativeAndroid.Controls.Dialogs;

/// <summary>
/// Android's runtime permission prompt, drawn by the system's permission controller over the app.
/// Its own scope: while it is shown the app is covered, so it never waits for the app.
/// </summary>
/// <typeparam name="TParent">The scope it is declared in, usually the app root.</typeparam>
public class PermissionDialog<TParent> : ContainerObjectBase<TParent, PermissionDialog<TParent>>
    where TParent : INativeAndroidScope<TParent>
{
    private static readonly Locator Frame = Locator.ByXPath(
        "//*[contains(@resource-id, 'permissioncontroller:id/grant_dialog')]");

    /// <summary>The permission prompt shown over <paramref name="parent"/>.</summary>
    public PermissionDialog(INativeAndroidScope<TParent> parent)
        : base(parent, Frame)
    {
    }

    /// <inheritdoc />
    public override PermissionDialog<TParent> Self => this;

    /// <summary>The system dialog covers the app, which is then not ready: it must not ask it.</summary>
    protected override bool AsksParent => false;

    /// <summary>What the app asks for.</summary>
    public TextView<PermissionDialog<TParent>> Message => new(this, Id("permission_message"));

    /// <summary>"Allow", or "While using the app" where Android offers that instead.</summary>
    public Button<PermissionDialog<TParent>> Allow
        => new(this, Locator.ByXPath(".//*[contains(@resource-id, ':id/permission_allow_button') or contains(@resource-id, ':id/permission_allow_foreground_only_button')]"));

    /// <summary>"Only this time".</summary>
    public Button<PermissionDialog<TParent>> AllowOnce => new(this, Id("permission_allow_one_time_button"));

    /// <summary>"Don't allow".</summary>
    public Button<PermissionDialog<TParent>> Deny
        => new(this, Locator.ByXPath(".//*[contains(@resource-id, ':id/permission_deny_button') or contains(@resource-id, ':id/permission_deny_and_dont_ask_again_button')]"));

    private static Locator Id(string name) => Locator.ByXPath($".//*[contains(@resource-id, ':id/{name}')]");
}
