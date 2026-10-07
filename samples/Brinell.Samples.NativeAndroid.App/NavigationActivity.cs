using Android.App;
using Android.Content;
using Android.Widget;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Tabs;

namespace Brinell.Samples.NativeAndroid.App;

/// <summary>Tabs, a bottom navigation bar and toolbar actions; the status says where the user is.</summary>
[Activity(Name = "com.brinell.samples.nativeandroid.NavigationActivity", Label = "Navigation", Exported = true)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = Scheme, DataHost = "navigation")]
public sealed class NavigationActivity : GalleryActivity
{
    private string _tab = "One";
    private string _section = "Home";
    private TextView _status = null!;

    protected override int Layout => Resource.Layout.navigation;

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _status = FindViewById<TextView>(Resource.Id.status)!;

        var tabs = FindViewById<TabLayout>(Resource.Id.tabs)!;
        foreach (var name in new[] { "One", "Two", "Three" })
        {
            tabs.AddTab(tabs.NewTab().SetText(name));
        }

        tabs.TabSelected += (_, e) =>
        {
            _tab = e.Tab!.Text!;
            Show();
        };

        var bottom = FindViewById<BottomNavigationView>(Resource.Id.bottom_nav)!;
        bottom.InflateMenu(Resource.Menu.navigation_bottom);
        bottom.ItemSelected += (_, e) =>
        {
            _section = e.Item.TitleFormatted!.ToString();
            Show();
            e.Handled = true;
        };

        var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar)!;
        toolbar.InflateMenu(Resource.Menu.toolbar);
        toolbar.MenuItemClick += (_, e) => _status.Text = $"{e.Item!.TitleFormatted} chosen";
    }

    private void Show() => _status.Text = $"Tab {_tab}, {_section}";
}
