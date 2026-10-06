using System.Drawing;
using Brinell.Core.Recording;

namespace Brinell.Maui.Interfaces;

/// <summary>A MAUI gesture as a recorded video's touch mark (<see cref="Interactions"/>).</summary>
public static class MauiInteractions
{
    /// <summary>Reports a gesture about to run on a target; a pan or a pinch leaves no mark.</summary>
    /// <param name="gesture">The gesture.</param>
    /// <param name="bounds">Reads the target's rectangle; only called while recording.</param>
    /// <param name="label">Reads the target's AutomationId; only called while recording.</param>
    public static Interactions.Scope Report(MauiGesture gesture, Func<Rectangle> bounds, Func<string?>? label = null) => gesture switch
    {
        MauiGesture.Tap => Interactions.Report(InteractionKind.Tap, bounds, label),
        MauiGesture.DoubleTap => Interactions.Report(InteractionKind.DoubleTap, bounds, label),
        MauiGesture.LongPress => Interactions.Report(InteractionKind.LongPress, bounds, label),
        MauiGesture.SwipeLeft => Interactions.ReportSwipe(bounds, -1, 0, label),
        MauiGesture.SwipeRight => Interactions.ReportSwipe(bounds, 1, 0, label),
        MauiGesture.SwipeUp => Interactions.ReportSwipe(bounds, 0, -1, label),
        MauiGesture.SwipeDown => Interactions.ReportSwipe(bounds, 0, 1, label),
        _ => default,
    };
}
