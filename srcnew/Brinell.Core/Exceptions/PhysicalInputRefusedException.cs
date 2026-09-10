namespace Brinell.Core.Exceptions;

/// <summary>
/// Thrown when real mouse, keyboard or clipboard input is attempted while background mode is on.
/// </summary>
/// <remarks>
/// Names both halves on purpose. The call site says which code reached for the mouse, and the
/// replacement says what it should have used instead — so the failure reads as a work item rather
/// than a puzzle.
/// </remarks>
public class PhysicalInputRefusedException : Exception
{
    /// <summary>
    /// Creates the exception for a refused call site.
    /// </summary>
    /// <param name="callSite">What was about to use real input.</param>
    /// <param name="replacement">The semantic route that should replace it.</param>
    public PhysicalInputRefusedException(string callSite, string replacement)
        : base($"'{callSite}' needs real mouse, keyboard or clipboard input, which background " +
               $"mode refuses: it is desktop-global and shared with whoever is at the keyboard. " +
               $"Use {replacement} instead, or clear BRINELL_BACKGROUND_MODE to allow it.")
    {
        CallSite = callSite;
        Replacement = replacement;
    }

    /// <summary>What was about to use real input.</summary>
    public string CallSite { get; }

    /// <summary>The semantic route that should replace it.</summary>
    public string Replacement { get; }
}
