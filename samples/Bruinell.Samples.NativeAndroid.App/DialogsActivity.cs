using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Google.Android.Material.Dialog;
using Google.Android.Material.Snackbar;

namespace Bruinell.Samples.NativeAndroid.App;

/// <summary>An alert dialog, the camera permission prompt, a snackbar, and a long piece of work.</summary>
[Activity(Name = "com.bruinell.samples.nativeandroid.DialogsActivity", Label = "Dialogs", Exported = true)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = Scheme, DataHost = "dialogs")]
public sealed class DialogsActivity : GalleryActivity
{
    private const int CameraRequest = 7;

    private TextView _status = null!;

    protected override int Layout => Resource.Layout.dialogs;

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _status = FindViewById<TextView>(Resource.Id.status)!;

        FindViewById<Button>(Resource.Id.show_alert)!.Click += (_, _) => new MaterialAlertDialogBuilder(this)
            .SetTitle("Delete everything?")!
            .SetMessage("This cannot be undone.")!
            .SetPositiveButton("Delete", (_, _) => _status.Text = "Deleted")!
            .SetNegativeButton("Cancel", (_, _) => _status.Text = "Kept")!
            .Show();

        FindViewById<Button>(Resource.Id.ask_camera)!.Click += (_, _) =>
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Permission.Granted)
            {
                _status.Text = "Camera allowed";
                return;
            }

            ActivityCompat.RequestPermissions(this, [Manifest.Permission.Camera], CameraRequest);
        };

        FindViewById<Button>(Resource.Id.show_snackbar)!.Click += (sender, _) =>
        {
            var archived = Snackbar.Make((Android.Views.View)sender!, "Archived", Snackbar.LengthLong)
                .SetAction("Undo", _ => _status.Text = "Unarchived");
            archived.SetDuration(SnackbarMs);
            archived.Show();
        };

        FindViewById<Button>(Resource.Id.work)!.Click += (_, _) => Busy(2000, () => _status.Text = "Work done");
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == CameraRequest)
        {
            _status.Text = grantResults.FirstOrDefault() == Permission.Granted ? "Camera allowed" : "Camera denied";
        }
    }
}
