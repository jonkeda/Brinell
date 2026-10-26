using Android.App;
using Android.Content;
using Android.Widget;

namespace Bruinet.Samples.NativeAndroid.App;

/// <summary>The gallery's start screen: a button per screen.</summary>
[Activity(Name = "com.bruinet.samples.nativeandroid.MainActivity", Label = "Bruinet Native", MainLauncher = true, Exported = true)]
public sealed class MainActivity : GalleryActivity
{
    protected override int Layout => Resource.Layout.main;

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Open(Resource.Id.open_login, typeof(LoginActivity));
        Open(Resource.Id.open_controls, typeof(ControlsActivity));
        Open(Resource.Id.open_list, typeof(InvoicesActivity));
        Open(Resource.Id.open_dialogs, typeof(DialogsActivity));
        Open(Resource.Id.open_navigation, typeof(NavigationActivity));
    }

    private void Open(int button, Type screen)
        => FindViewById<Button>(button)!.Click += (_, _) => StartActivity(new Intent(this, screen));
}
