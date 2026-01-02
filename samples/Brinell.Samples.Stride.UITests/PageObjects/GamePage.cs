using Brinell.Core.Abstractions;
using Brinell.Stride.Controls;
using Brinell.Stride.Infrastructure;
using Brinell.Stride.Pages;

namespace Brinell.Samples.Stride.UITests.PageObjects;

/// <summary>
/// Page object for the main game view.
/// </summary>
public class GamePage : StridePageBase
{
    /// <inheritdoc />
    public override string Name => "Game Page";

    public GamePage(StrideTestContext context) : base(context, "HUD")
    {
    }

    #region HUD Controls

    /// <summary>
    /// Game title text block.
    /// </summary>
    public StrideTextBlockControl GameTitle => TextBlock("GameTitle");

    /// <summary>
    /// Player position display text block.
    /// </summary>
    public StrideTextBlockControl PositionDisplay => TextBlock("PositionDisplay");

    /// <summary>
    /// ESC hint text block.
    /// </summary>
    public StrideTextBlockControl EscHint => TextBlock("EscHint");

    /// <summary>
    /// Movement hint text block.
    /// </summary>
    public StrideTextBlockControl MovementHint => TextBlock("MovementHint");

    #endregion

    #region Actions

    /// <summary>
    /// Open the settings page using ESC key.
    /// </summary>
    public void OpenSettings()
    {
        PressKey(VirtualKey.Escape);
    }

    /// <summary>
    /// Move player north (W key) for specified duration.
    /// </summary>
    public void MoveNorth(int durationMs = 500)
    {
        HoldKey(VirtualKey.W, durationMs);
    }

    /// <summary>
    /// Move player south (S key) for specified duration.
    /// </summary>
    public void MoveSouth(int durationMs = 500)
    {
        HoldKey(VirtualKey.S, durationMs);
    }

    /// <summary>
    /// Move player east (D key) for specified duration.
    /// </summary>
    public void MoveEast(int durationMs = 500)
    {
        HoldKey(VirtualKey.D, durationMs);
    }

    /// <summary>
    /// Move player west (A key) for specified duration.
    /// </summary>
    public void MoveWest(int durationMs = 500)
    {
        HoldKey(VirtualKey.A, durationMs);
    }

    #endregion
}
