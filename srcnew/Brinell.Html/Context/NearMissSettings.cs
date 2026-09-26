namespace Brinell.Html.Context;

/// <summary>
/// When a call that passed is logged as a near-miss (a <c>Warning</c> starting "near-miss:"):
/// its element was replaced <paramref name="Replacements"/> times or more, or it needed more than
/// one attempt and more than <paramref name="BudgetShare"/> of its budget. MAUI's defaults.
/// </summary>
public sealed record NearMissSettings(int Replacements = 3, double BudgetShare = 0.5)
{
    /// <summary>The defaults.</summary>
    public static NearMissSettings Default { get; } = new();
}
