using Brinell.Core.Logging;

namespace Brinell.Core.Calls;

/// <summary>
/// When a call that succeeded is still reported, as a near-miss (see
/// <c>.docs/contracts/call-model.md</c>, R0): its element was replaced
/// <paramref name="Replacements"/> times or more, or it needed more than one attempt and more than
/// <paramref name="BudgetShare"/> of its budget.
/// </summary>
/// <remarks>
/// A near-miss is logged as <see cref="LogResult.Warning"/> with a "near-miss:" summary, so a UI
/// that keeps re-rendering, or a page slow to become idle, shows up although the test passed.
/// </remarks>
/// <param name="Replacements">Element replacements seen during the call at or above which it is a near-miss.</param>
/// <param name="BudgetShare">The share of the call's budget above which it is a near-miss.</param>
public sealed record NearMissSettings(int Replacements = 3, double BudgetShare = 0.5)
{
    /// <summary>3 or more replacements, or more than half the budget used.</summary>
    public static NearMissSettings Default { get; } = new();
}
