using Brinell.Presenter.ViewModels;

namespace Brinell.Presenter.Controls;

/// <summary>
/// The default expand/collapse affordance of a <see cref="TreeViewNode"/>: a chevron that points
/// right when collapsed and down when expanded, and blank space for a node with no children.
/// </summary>
public class ExpandButtonContent : ContentView
{
    /// <summary>The width the glyph reserves, so rows at one depth line up whatever their state.</summary>
    public const double GlyphWidth = 20;

    private readonly Label _glyph;

    /// <summary>Creates the affordance in its collapsed, non-expandable state.</summary>
    public ExpandButtonContent()
    {
        _glyph = new Label
        {
            FontFamily = IconFont,
            FontSize = 12,
            WidthRequest = GlyphWidth,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        };
        _glyph.SetAppThemeColor(Label.TextColorProperty, MutedLight, MutedDark);

        Content = _glyph;
        VerticalOptions = LayoutOptions.Center;
        UpdateGlyph();
    }

    /// <summary>Whether the node this belongs to is currently expanded.</summary>
    public static readonly BindableProperty IsExpandedProperty = BindableProperty.Create(
        nameof(IsExpanded), typeof(bool), typeof(ExpandButtonContent), false,
        propertyChanged: (bindable, _, _) => ((ExpandButtonContent)bindable).UpdateGlyph());

    /// <summary>Whether the node has children to show.</summary>
    public static readonly BindableProperty IsExpandableProperty = BindableProperty.Create(
        nameof(IsExpandable), typeof(bool), typeof(ExpandButtonContent), false,
        propertyChanged: (bindable, _, _) => ((ExpandButtonContent)bindable).UpdateGlyph());

    /// <summary>Whether a childless node still draws a (collapsed) chevron.</summary>
    public static readonly BindableProperty ShowGlyphIfEmptyProperty = BindableProperty.Create(
        nameof(ShowGlyphIfEmpty), typeof(bool), typeof(ExpandButtonContent), false,
        propertyChanged: (bindable, _, _) => ((ExpandButtonContent)bindable).UpdateGlyph());

    /// <inheritdoc cref="IsExpandedProperty"/>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <inheritdoc cref="IsExpandableProperty"/>
    public bool IsExpandable
    {
        get => (bool)GetValue(IsExpandableProperty);
        set => SetValue(IsExpandableProperty, value);
    }

    /// <inheritdoc cref="ShowGlyphIfEmptyProperty"/>
    public bool ShowGlyphIfEmpty
    {
        get => (bool)GetValue(ShowGlyphIfEmptyProperty);
        set => SetValue(ShowGlyphIfEmptyProperty, value);
    }

    private static string IconFont =>
        Application.Current?.Resources.TryGetValue("PresenterIconFont", out var font) == true && font is string name
            ? name
            : "Segoe Fluent Icons";

    private static Color MutedLight => TreeViewColors.Resource("PresenterMutedLight", "#2B3340");

    private static Color MutedDark => TreeViewColors.Resource("PresenterMutedDark", "#C5CCDA");

    private void UpdateGlyph()
    {
        _glyph.Text = (IsExpandable, ShowGlyphIfEmpty) switch
        {
            (true, _) => IsExpanded ? PresenterIcons.ChevronDown : PresenterIcons.ChevronRight,
            (false, true) => PresenterIcons.ChevronRight,
            _ => string.Empty
        };
        _glyph.Opacity = IsExpandable ? 1 : 0.35;
    }
}
