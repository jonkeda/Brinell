namespace Brinell.Samples.Todo.App.Views;

/// <summary>
/// A bordered card with a header, around content the page chooses.
/// </summary>
/// <remarks>
/// <para>
/// <b>A container.</b> The card fixes only its header (<see cref="HeaderId"/>); what is inside is
/// the page's business. Brinell's <c>SectionCard</c> ControlObject scopes lookups to the card, so a
/// page can reuse ids like <c>DueValue</c> in different cards without them clashing.
/// </para>
/// <para>
/// A <see cref="ContentView"/>, so the AppSupport handler makes its AutomationId visible to UI
/// Automation on Windows.
/// </para>
/// </remarks>
[ContentProperty(nameof(Body))]
public sealed class SectionCard : ContentView
{
    /// <summary>The header label's id.</summary>
    public const string HeaderId = "SectionHeader";

    /// <summary>The header text.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(
        nameof(Header), typeof(string), typeof(SectionCard), string.Empty,
        propertyChanged: (card, _, value) => ((SectionCard)card)._header.Text = (string)value);

    /// <summary>The card's content.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body), typeof(View), typeof(SectionCard), null,
        propertyChanged: (card, _, value) => ((SectionCard)card)._body.Content = (View?)value);

    private readonly Label _header;
    private readonly ContentView _body;

    public SectionCard()
    {
        _header = new Label { AutomationId = HeaderId, FontAttributes = FontAttributes.Bold, FontSize = 16 };
        _body = new ContentView();

        Content = new Border
        {
            Padding = 12,
            StrokeThickness = 1,
            Stroke = Colors.Gray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { _header, _body },
            },
        };
    }

    /// <inheritdoc cref="HeaderProperty"/>
    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <inheritdoc cref="BodyProperty"/>
    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }
}
