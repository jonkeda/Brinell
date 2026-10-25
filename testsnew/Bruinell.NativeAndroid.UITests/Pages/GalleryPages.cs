using Bruinell.NativeAndroid.Controls.Buttons;
using Bruinell.NativeAndroid.Controls.Collection;
using Bruinell.NativeAndroid.Controls.Container;
using Bruinell.NativeAndroid.Controls.Dialogs;
using Bruinell.NativeAndroid.Controls.Display;
using Bruinell.NativeAndroid.Controls.Navigation;
using Bruinell.NativeAndroid.Controls.Range;
using Bruinell.NativeAndroid.Controls.Selection;
using Bruinell.NativeAndroid.Controls.Text;
using Bruinell.NativeAndroid.Controls.Toggle;

namespace Bruinell.NativeAndroid.UITests.Pages;

/// <summary>The sign-in screen.</summary>
[TestPage("Sign In")]
public sealed class LoginPage(INativeAndroidTestContext context) : PageObjectBase<LoginPage>(context)
{
    public override string? Activity => ".LoginActivity";

    protected override Locator? ReadyLocator => Locator.ById("login");

    public EditText<LoginPage> UserName => new(this, "username");

    public EditText<LoginPage> Password => new(this, "password");

    public Button<LoginPage> SignIn => new(this, "login");

    public TextView<LoginPage> Status => new(this, "status");

    public Toolbar<LoginPage> Toolbar => new(this, "toolbar");
}

/// <summary>One of each input control; the status line says what changed last.</summary>
[TestPage("Controls")]
public sealed class ControlsPage(INativeAndroidTestContext context) : PageObjectBase<ControlsPage>(context)
{
    public override string? Activity => ".ControlsActivity";

    protected override Locator? ReadyLocator => Locator.ById("status");

    public TextView<ControlsPage> Status => new(this, "status");

    public CheckBox<ControlsPage> Agree => new(this, "agree");

    public Switch<ControlsPage> Notifications => new(this, "notifications");

    public ToggleButton<ControlsPage> Bold => new(this, "bold");

    public Chip<ControlsPage> Express => new(this, "express");

    public RadioGroup<ControlsPage> Size => new(this, "size");

    public Spinner<ControlsPage> Color => new(this, "color");

    public SeekBar<ControlsPage> Volume => new(this, "volume", maximum: 100);

    public RatingBar<ControlsPage> Rating => new(this, "rating");

    public AutoCompleteTextView<ControlsPage> Country => new(this, "country");

    public EditText<ControlsPage> Notes => new(this, "notes");

    public ImageView<ControlsPage> Logo => new(this, "logo");

    public ScrollView<ControlsPage> Scroll => new(this, "scroll");

    public Button<ControlsPage> BottomButton => new(this, "bottom_button");
}

/// <summary>200 invoices in a recycler view.</summary>
[TestPage("Invoices")]
public sealed class InvoicesPage(INativeAndroidTestContext context) : PageObjectBase<InvoicesPage>(context)
{
    public override string? Activity => ".InvoicesActivity";

    protected override Locator? ReadyLocator => Locator.ById("invoices");

    public TextView<InvoicesPage> Count => new(this, "count");

    public RecyclerView<InvoicesPage> Invoices => new(this, "invoices", itemKeyId: "name");

    public SwipeRefreshLayout<InvoicesPage> Refresh => new(this, "refresh");

    public Snackbar<InvoicesPage> Snackbar => new(this);
}

/// <summary>An alert dialog, the camera permission prompt, a snackbar, and a long piece of work.</summary>
[TestPage("Dialogs")]
public sealed class DialogsPage(INativeAndroidTestContext context) : PageObjectBase<DialogsPage>(context)
{
    public override string? Activity => ".DialogsActivity";

    protected override Locator? ReadyLocator => Locator.ById("status");

    public Button<DialogsPage> ShowAlert => new(this, "show_alert");

    public Button<DialogsPage> AskCamera => new(this, "ask_camera");

    public Button<DialogsPage> ShowSnackbar => new(this, "show_snackbar");

    public Button<DialogsPage> Work => new(this, "work");

    public TextView<DialogsPage> Status => new(this, "status");

    public AlertDialog<DialogsPage> Alert => new(this);

    public PermissionDialog<DialogsPage> Permission => new(this);

    public Snackbar<DialogsPage> Snackbar => new(this);
}

/// <summary>Tabs, a bottom navigation bar and toolbar actions.</summary>
[TestPage("Navigation")]
public sealed class NavigationPage(INativeAndroidTestContext context) : PageObjectBase<NavigationPage>(context)
{
    public override string? Activity => ".NavigationActivity";

    protected override Locator? ReadyLocator => Locator.ById("tabs");

    public Toolbar<NavigationPage> Toolbar => new(this, "toolbar");

    public TabLayout<NavigationPage> Tabs => new(this, "tabs");

    public BottomNavigationView<NavigationPage> Sections => new(this, "bottom_nav");

    public TextView<NavigationPage> Status => new(this, "status");
}
