namespace Brinell.Maui.Context;

/// <summary>
/// When a call that succeeded is still reported, as a near-miss (<c>.my/stale-readiness/design.md</c>,
/// R0 and X5).
/// </summary>
/// <remarks>
/// A near-miss is logged as <see cref="LogResult.Warning"/> with a "near-miss:" summary, so a UI
/// that keeps re-rendering, or a page slow to become idle, shows up although the test passed.
/// Step 8 of the plan measured the defaults: 1 near-miss in 4,833 logged calls on the full Windows
/// suite, which is not noise.
/// </remarks>
/// <param name="Replacements">Element replacements seen during the call at or above which it is a near-miss.</param>
/// <param name="BudgetShare">The share of the call's budget above which it is a near-miss.</param>
public sealed record NearMissSettings(int Replacements = 3, double BudgetShare = 0.5)
{
    /// <summary>3 or more replacements, or more than half the budget used.</summary>
    public static NearMissSettings Default { get; } = new();
}
