using System.Globalization;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MUnit.Inspection;

/// <summary>
/// How MUnit reads MAUI's own controls: the page a container shows, a page's toolbar, the rows an
/// items view would show, and each control's text.
/// </summary>
public sealed class DefaultElementAdapter : IElementAdapter
{
    public static readonly DefaultElementAdapter Instance = new();

    public bool CanAdapt(Element element) => true;

    public IEnumerable<Element> GetChildren(Element element)
    {
        switch (element)
        {
            case Window window:
                return window.Page is { } root ? [Screen.VisiblePage(root, window)] : [];
            case NavigationPage navigation:
                return navigation.CurrentPage is { } current ? [current] : [];
            case Shell shell:
                return Screen.CurrentPage(shell) is { } shown ? [shown] : [];
            case TabbedPage tabbed:
                return tabbed.CurrentPage is { } tab ? [tab] : [];
            case FlyoutPage flyout:
                return flyout.IsPresented ? [flyout.Flyout, flyout.Detail] : [flyout.Detail];
            case ItemsView items:
                return ItemRealizer.Realize(items);
            case Page page:
                return page.ToolbarItems.Cast<Element>().Concat(VisualChildren(page));
            default:
                return VisualChildren(element);
        }
    }

    public string? GetText(Element element) => element switch
    {
        Label { FormattedText: { } formatted } => string.Concat(formatted.Spans.Select(span => span.Text)),
        Label label => label.Text,
        Span span => span.Text,
        Button button => button.Text,
        RadioButton radio => radio.Content as string ?? radio.ContentAsString(),
        InputView input => input.Text,
        Picker picker => picker.SelectedIndex >= 0 && picker.SelectedIndex < picker.Items.Count
            ? picker.Items[picker.SelectedIndex]
            : null,
        DatePicker date => date.Date?.ToString(date.Format, CultureInfo.CurrentCulture),
        TimePicker time => time.Time is { } value
            ? DateTime.Today.Add(value).ToString(time.Format, CultureInfo.CurrentCulture)
            : null,
        MenuItem item => item.Text,
        Page page => page.Title,
        IText text => text.Text,
        _ => null,
    };

    private static IEnumerable<Element> VisualChildren(Element element)
        => ((IVisualTreeElement)element).GetVisualChildren().OfType<Element>();
}
