using CommunityToolkit.Maui.Layouts;
using CommunityToolkit.Maui.Views;

namespace Brinell.Samples.Maui.App.Views2.TestViews;

/// <summary>
/// Code-behind for the CommunityToolkit views demo.
/// </summary>
/// <remarks>
/// Each handler echoes the control's state into a label, so a test asserts what a user sees
/// rather than reading the control's properties.
/// </remarks>
public partial class CommunityToolkitView : ContentView
{
    public CommunityToolkitView()
    {
        InitializeComponent();
    }

    private void OnExpandedChanged(object? sender, EventArgs e)
    {
        if (sender is Expander expander)
        {
            ExpanderStatus.Text = expander.IsExpanded ? "expanded" : "collapsed";
        }
    }

    private void OnRatingChanged(object? sender, EventArgs e)
    {
        if (sender is RatingView rating)
        {
            RatingStatus.Text = $"rating: {rating.Rating:0.##}";
        }
    }

    private void OnDrawingLineCompleted(object? sender, EventArgs e)
        => DrawingStatus.Text = $"lines: {Drawing.Lines.Count}";

    private void OnClearDrawing(object? sender, EventArgs e)
    {
        Drawing.Clear();
        DrawingStatus.Text = "lines: 0";
    }

    private void OnStateLoading(object? sender, EventArgs e) => SetState("Loading");

    private void OnStateError(object? sender, EventArgs e) => SetState("Error");

    private void OnStateContent(object? sender, EventArgs e) => SetState(null);

    private void SetState(string? state)
    {
        StateContainer.SetCurrentState(StateHost, state);
        StateStatus.Text = $"state: {state?.ToLowerInvariant() ?? "content"}";
    }
}
