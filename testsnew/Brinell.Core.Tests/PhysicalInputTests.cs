using Brinell.Core.Diagnostics;
using Brinell.Core.Exceptions;

namespace Brinell.Core.Tests;

/// <summary>
/// Covers the physical-input gate: its default, both active policies, and the message it produces.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately here rather than beside a driver. The gate is platform-neutral and guards the
/// WPF and WinForms drivers as well as the MAUI one, so pinning it against a single platform's UI
/// suite would test the least of it — and would need an app, a desktop and a minute per run to say
/// something these say in milliseconds.
/// </para>
/// <para>
/// What each platform driver still owes is that its own call sites are guarded. That is a
/// different question, answered by an audit run against a real app, not by this file.
/// </para>
/// </remarks>
public class PhysicalInputTests
{
    [Fact]
    public void Policy_DefaultsToAllowed_SoANormalRunIsUnchanged()
    {
        Assert.Equal(PhysicalInputPolicy.Allowed, PhysicalInput.Policy);
    }

    [Fact]
    public void Used_RecordsNothing_WhileAllowed()
    {
        PhysicalInput.ResetRecorded();

        PhysicalInput.Used("Some.CallSite", "some verb");

        Assert.Empty(PhysicalInput.Uses);
    }

    [Fact]
    public void Used_RecordsAndContinues_WhileAuditing()
    {
        PhysicalInput.ResetRecorded();

        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Audited))
        {
            PhysicalInput.Used("Driver.Click", "an activation pattern");
            PhysicalInput.Used("Driver.Click", "an activation pattern");
            PhysicalInput.Used("Driver.Hover", "a pointer-enter verb");
        }

        Assert.Equal(2, PhysicalInput.Uses["Driver.Click"].Count);
        Assert.Equal(1, PhysicalInput.Uses["Driver.Hover"].Count);
        Assert.Equal("an activation pattern", PhysicalInput.Uses["Driver.Click"].Replacement);
    }

    /// <summary>
    /// Auditing must not change what the suite does, or the measurement is not of the suite.
    /// </summary>
    [Fact]
    public void Used_DoesNotThrow_WhileAuditing()
    {
        PhysicalInput.ResetRecorded();

        using var scope = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Audited);

        PhysicalInput.Used("Driver.Click", "an activation pattern");
    }

    [Fact]
    public void Used_Throws_WhileRefusing()
    {
        PhysicalInput.ResetRecorded();

        using var scope = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused);

        var exception = Assert.Throws<PhysicalInputRefusedException>(
            () => PhysicalInput.Used("Driver.Click", "an activation pattern"));

        Assert.Equal("Driver.Click", exception.CallSite);
        Assert.Equal("an activation pattern", exception.Replacement);
    }

    /// <summary>
    /// A refused call is still recorded, so a strict run reports what it stopped.
    /// </summary>
    [Fact]
    public void Used_RecordsBeforeThrowing_WhileRefusing()
    {
        PhysicalInput.ResetRecorded();

        using var scope = PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused);

        Assert.Throws<PhysicalInputRefusedException>(
            () => PhysicalInput.Used("Driver.Click", "an activation pattern"));

        Assert.Equal(1, PhysicalInput.Uses["Driver.Click"].Count);
    }

    [Fact]
    public void OverridePolicy_RestoresThePreviousPolicy_WhenDisposed()
    {
        using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Refused))
        {
            Assert.Equal(PhysicalInputPolicy.Refused, PhysicalInput.Policy);

            using (PhysicalInput.OverridePolicy(PhysicalInputPolicy.Audited))
            {
                Assert.Equal(PhysicalInputPolicy.Audited, PhysicalInput.Policy);
            }

            Assert.Equal(PhysicalInputPolicy.Refused, PhysicalInput.Policy);
        }

        Assert.Equal(PhysicalInputPolicy.Allowed, PhysicalInput.Policy);
    }

    /// <summary>
    /// The message names the offending call site and its replacement, in that order.
    /// </summary>
    /// <remarks>
    /// Both halves matter. Without the call site the failure is a puzzle; without the replacement
    /// it is a complaint. The inventory this feeds is only useful because every entry says what to
    /// build instead.
    /// </remarks>
    [Fact]
    public void RefusedException_NamesBothTheCallSiteAndItsReplacement()
    {
        var exception = new PhysicalInputRefusedException(
            "FlaUIMauiElement.SendKeys(Paste)", "the SetText verb");

        Assert.Contains("FlaUIMauiElement.SendKeys(Paste)", exception.Message);
        Assert.Contains("the SetText verb", exception.Message);
        Assert.Contains("BRINELL_BACKGROUND_MODE", exception.Message);
    }
}
