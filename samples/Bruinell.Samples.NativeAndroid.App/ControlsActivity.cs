using System.Globalization;
using Android.App;
using Android.Content;
using Android.Widget;
using Google.Android.Material.Chip;
using Google.Android.Material.MaterialSwitch;

namespace Bruinell.Samples.NativeAndroid.App;

/// <summary>One of each input control; the status line says what changed last.</summary>
[Activity(Name = "com.bruinell.samples.nativeandroid.ControlsActivity", Label = "Controls", Exported = true)]
[IntentFilter([Intent.ActionView], Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable], DataScheme = Scheme, DataHost = "controls")]
public sealed class ControlsActivity : GalleryActivity
{
    private TextView _status = null!;

    protected override int Layout => Resource.Layout.controls;

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _status = FindViewById<TextView>(Resource.Id.status)!;

        FindViewById<CheckBox>(Resource.Id.agree)!.CheckedChange += (_, e) => Say($"Agree {On(e.IsChecked)}");
        FindViewById<MaterialSwitch>(Resource.Id.notifications)!.CheckedChange += (_, e) => Say($"Notifications {On(e.IsChecked)}");
        FindViewById<ToggleButton>(Resource.Id.bold)!.CheckedChange += (_, e) => Say($"Bold {On(e.IsChecked)}");
        FindViewById<Chip>(Resource.Id.express)!.CheckedChange += (_, e) => Say($"Express {On(e.IsChecked)}");
        FindViewById<RadioGroup>(Resource.Id.size)!.CheckedChange += (sender, e) => Say($"Size {FindViewById<RadioButton>(e.CheckedId)?.Text}");
        // A spinner reports its first item as selected when it is laid out; that is not a change.
        var colorShown = false;
        FindViewById<Spinner>(Resource.Id.color)!.ItemSelected += (sender, e) =>
        {
            if (colorShown)
            {
                Say($"Color {((Spinner)sender!).SelectedItem}");
            }

            colorShown = true;
        };
        FindViewById<SeekBar>(Resource.Id.volume)!.ProgressChanged += (_, e) => Say($"Volume {e.Progress}");
        FindViewById<RatingBar>(Resource.Id.rating)!.RatingBarChange += (_, e) => Say($"Rating {e.Rating.ToString(CultureInfo.InvariantCulture)}");
        FindViewById<Button>(Resource.Id.bottom_button)!.Click += (_, _) => Say("Bottom tapped");

        var countries = FindViewById<AutoCompleteTextView>(Resource.Id.country)!;
        countries.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleDropDownItem1Line, Resources!.GetStringArray(Resource.Array.countries));
        countries.ItemClick += (_, e) => Say($"Country {countries.Adapter.GetItem(e.Position)}");
    }

    private void Say(string text) => _status.Text = text;

    private static string On(bool on) => on ? "on" : "off";
}
