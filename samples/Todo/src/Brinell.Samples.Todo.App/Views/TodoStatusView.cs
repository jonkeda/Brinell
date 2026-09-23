using Brinell.Samples.Todo.Core.Models;
using Brinell.Samples.Todo.Core.Rules;

namespace Brinell.Samples.Todo.App.Views;

/// <summary>
/// A todo's status: a glyph, its name, and - unless read-only - a Next button that cycles it.
/// </summary>
/// <remarks>
/// <para>
/// <b>A component.</b> One control to the user, made of parts whose ids the control fixes itself:
/// <see cref="GlyphId"/>, <see cref="TextId"/> and <see cref="NextId"/>. The page gives the whole
/// control its AutomationId; the parts' ids are only unique inside it, which is what lets the same
/// control repeat in every list row. Brinell's <c>TodoStatus</c> ControlObject addresses the parts
/// by these ids, scoped to the root.
/// </para>
/// <para>
/// Overdue is shown, not stored: when <see cref="IsPastDue"/> and the status is not Done.
/// </para>
/// </remarks>
public sealed class TodoStatusView : ContentView
{
    /// <summary>The glyph label's id.</summary>
    public const string GlyphId = "StatusGlyph";

    /// <summary>The status name label's id.</summary>
    public const string TextId = "StatusText";

    /// <summary>The Next button's id.</summary>
    public const string NextId = "StatusNext";

    /// <summary>The status. Two-way: Next writes it back.</summary>
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(TodoStatus), typeof(TodoStatusView), TodoStatus.Open, BindingMode.TwoWay,
        propertyChanged: (view, _, _) => ((TodoStatusView)view).Refresh());

    /// <summary>Whether the due date has passed.</summary>
    public static readonly BindableProperty IsPastDueProperty = BindableProperty.Create(
        nameof(IsPastDue), typeof(bool), typeof(TodoStatusView), false,
        propertyChanged: (view, _, _) => ((TodoStatusView)view).Refresh());

    /// <summary>Whether Next is hidden.</summary>
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(
        nameof(IsReadOnly), typeof(bool), typeof(TodoStatusView), false,
        propertyChanged: (view, _, _) => ((TodoStatusView)view).Refresh());

    private readonly Label _glyph;
    private readonly Label _text;
    private readonly Button _next;

    public TodoStatusView()
    {
        _glyph = new Label { AutomationId = GlyphId, FontSize = 18, VerticalOptions = LayoutOptions.Center };
        _text = new Label { AutomationId = TextId, VerticalOptions = LayoutOptions.Center };
        _next = new Button { AutomationId = NextId, Text = "Next", Padding = new Thickness(12, 4) };
        _next.Clicked += (_, _) => Status = TodoStatusRules.Next(Status);
        SemanticProperties.SetDescription(_next, "Next status");

        Content = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _glyph, _text, _next },
        };

        Refresh();
    }

    /// <inheritdoc cref="StatusProperty"/>
    public TodoStatus Status
    {
        get => (TodoStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <inheritdoc cref="IsPastDueProperty"/>
    public bool IsPastDue
    {
        get => (bool)GetValue(IsPastDueProperty);
        set => SetValue(IsPastDueProperty, value);
    }

    /// <inheritdoc cref="IsReadOnlyProperty"/>
    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private void Refresh()
    {
        var shown = TodoStatusRules.Display(Status, IsPastDue);

        _glyph.Text = TodoStatusRules.Glyph(shown);
        _text.Text = TodoStatusRules.Text(shown);
        _next.IsVisible = !IsReadOnly;
        SemanticProperties.SetDescription(this, "Status: " + _text.Text);
    }
}
