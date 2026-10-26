using Android.App;
using Android.Content;
using Android.Widget;

namespace Bruinet.Samples.NativeAndroid.App;

/// <summary>
/// Sign in: the button enables once both fields are filled; signing in is busy for a moment,
/// then greets the user. <c>bruinet-native://login?stuck=1</c> keeps the button disabled forever,
/// the app bug the R0 proof needs (plan step 10).
/// </summary>
[Activity(Name = "com.bruinet.samples.nativeandroid.LoginActivity", Label = "Sign in", Exported = true)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = Scheme, DataHost = "login")]
public sealed class LoginActivity : GalleryActivity
{
    protected override int Layout => Resource.Layout.login;

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var username = FindViewById<EditText>(Resource.Id.username)!;
        var password = FindViewById<EditText>(Resource.Id.password)!;
        var login = FindViewById<Button>(Resource.Id.login)!;
        var status = FindViewById<TextView>(Resource.Id.status)!;
        var stuck = Query("stuck") == "1";

        void Update() => login.Enabled = !stuck && username.Text?.Length > 0 && password.Text?.Length > 0;
        username.TextChanged += (_, _) => Update();
        password.TextChanged += (_, _) => Update();

        login.Click += (_, _) =>
        {
            status.Text = "Signing in";
            Busy(1500, () => status.Text = $"Welcome, {username.Text}");
        };
    }
}
