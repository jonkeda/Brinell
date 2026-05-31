namespace Brinell.WinForms.Uat.Tests.Pages;

[UatName("Login")]
public sealed class LoginUatPage : PageObjectBase<LoginUatPage>
{
    public LoginUatPage(IWinFormsTestContext context)
        : base(context)
    {
    }

    public TextBox<LoginUatPage> UsernameField => TextBox("txtUsername");

    public PasswordBox<LoginUatPage> PasswordField => PasswordBox("txtPassword");

    public CheckBox<LoginUatPage> RememberCheckBox => CheckBox("chkRemember");

    [UatName("Role")]
    public ComboBox<LoginUatPage> RoleCombo => ComboBox("cmbRole");

    public Button<LoginUatPage> LoginButton => Button("btnLogin");

    public Label<LoginUatPage> StatusLabel => Label("lblStatus");

    public override bool IsLoaded(int? timeoutMs = null)
    {
        return UsernameField.IsExists() && LoginButton.IsExists();
    }
}
