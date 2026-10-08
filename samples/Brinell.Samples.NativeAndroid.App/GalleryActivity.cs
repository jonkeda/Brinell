using Android.Views;
using AndroidX.AppCompat.App;
using Google.Android.Material.AppBar;

namespace Brinell.Samples.NativeAndroid.App;

/// <summary>
/// A gallery screen: its layout, its toolbar, and the busy overlay Brinell reads as the busy
/// signal (<c>brinell_busy</c>), shown while the screen works.
/// </summary>
public abstract class GalleryActivity : AppCompatActivity
{
    /// <summary>The deep link scheme: <c>brinell-native://login</c> opens the sign-in screen.</summary>
    public const string Scheme = "brinell-native";

    /// <summary>How long a snackbar shows: long enough for a person, and for a test to read it and answer.</summary>
    protected const int SnackbarMs = 8000;

    private View? _busy;

    /// <summary>The screen's layout resource.</summary>
    protected abstract int Layout { get; }

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Layout);
        _busy = FindViewById(Resource.Id.brinell_busy);
        if (FindViewById<MaterialToolbar>(Resource.Id.toolbar) is { } toolbar && this is not MainActivity)
        {
            toolbar.SetNavigationIcon(Resource.Drawable.abc_ic_ab_back_material);
            toolbar.NavigationContentDescription = "Navigate up";
            toolbar.NavigationClick += (_, _) => Finish();
        }
    }

    /// <summary>Shows the busy overlay for <paramref name="milliseconds"/>, then runs <paramref name="done"/>.</summary>
    protected void Busy(int milliseconds, Action done)
    {
        _busy!.Visibility = ViewStates.Visible;
        _busy.PostDelayed(() =>
        {
            _busy.Visibility = ViewStates.Gone;
            done();
        }, milliseconds);
    }

    /// <summary>A query parameter of the deep link that opened the screen.</summary>
    protected string? Query(string name) => Intent?.Data?.GetQueryParameter(name);
}
