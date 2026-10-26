using System.Collections;
using System.Globalization;
using Microsoft.Maui.Controls;
using MauiElement = Microsoft.Maui.Controls.Element;

namespace Bruinet.Maui.MauiUnit;

/// <summary>
/// The state names Bruinet's Windows bridge answers (<c>MauiVerbDispatcher.ReadState</c> in
/// <c>Bruinet.Maui.AppSupport</c>), answered the same way and in the same formats, read from the
/// element in process instead of over UI Automation.
/// </summary>
internal static class MauiUnitState
{
    /// <summary>The bridge's answer for <paramref name="property"/>, or null where it has no case.</summary>
    public static string? Read(MauiElement element, string property) => (property, element) switch
    {
        ("IsVisible", VisualElement visual) => visual.IsVisible.ToString(),
        ("IsEnabled", VisualElement visual) => visual.IsEnabled.ToString(),
        ("IsFocused", VisualElement visual) => visual.IsFocused.ToString(),
        ("AutomationId", _) => element.AutomationId ?? string.Empty,
        ("NavigationDepth", Page page) => page.Navigation?.NavigationStack is { } stack && stack.Contains(page)
            ? stack.Count.ToString(CultureInfo.InvariantCulture)
            : null,
        ("Source", Image image) => image.Source switch
        {
            null => string.Empty,
            FileImageSource file => file.File ?? string.Empty,
            UriImageSource uri => uri.Uri?.ToString() ?? string.Empty,
            _ => image.Source.GetType().Name,
        },
        ("IsLoading", Image image) => image.IsLoading.ToString(),
        ("Progress", ProgressBar bar) => bar.Progress.ToString(CultureInfo.InvariantCulture),
        ("Date", DatePicker picker) => picker.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty,
        ("Time", TimePicker picker) => picker.Time?.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) ?? string.Empty,
        ("Value", Stepper stepper) => stepper.Value.ToString(CultureInfo.InvariantCulture),
        ("Minimum", Stepper stepper) => stepper.Minimum.ToString(CultureInfo.InvariantCulture),
        ("Maximum", Stepper stepper) => stepper.Maximum.ToString(CultureInfo.InvariantCulture),
        ("Increment", Stepper stepper) => stepper.Increment.ToString(CultureInfo.InvariantCulture),
        ("SelectedIndex", Picker picker) => picker.SelectedIndex.ToString(CultureInfo.InvariantCulture),
        ("SelectedItem", Picker picker) => picker.SelectedIndex >= 0 && picker.SelectedIndex < picker.Items.Count
            ? picker.Items[picker.SelectedIndex]
            : string.Empty,
        ("ItemCount", Picker picker) => picker.Items.Count.ToString(CultureInfo.InvariantCulture),
        ("ItemCount", ItemsView items) => Count(items.ItemsSource).ToString(CultureInfo.InvariantCulture),
        ("Items", Picker picker) => string.Join("\n", new[] { picker.Items.Count.ToString(CultureInfo.InvariantCulture) }.Concat(picker.Items)),
        ("Position", CarouselView carousel) => carousel.Position.ToString(CultureInfo.InvariantCulture),
        ("Position", IndicatorView indicator) => indicator.Position.ToString(CultureInfo.InvariantCulture),
        ("Count", IndicatorView indicator) => indicator.Count.ToString(CultureInfo.InvariantCulture),
        ("FlyoutIsPresented", _) => ShellOf(element)?.FlyoutIsPresented.ToString(),
        _ => null,
    };

    private static int Count(IEnumerable? source) => source switch
    {
        null => 0,
        ICollection collection => collection.Count,
        _ => source.Cast<object>().Count(),
    };

    private static Shell? ShellOf(MauiElement element)
    {
        for (var walker = element; walker is not null; walker = walker.Parent)
        {
            if (walker is Shell shell)
                return shell;
            if (walker is Window window)
                return window.Page as Shell;
        }

        return null;
    }
}
