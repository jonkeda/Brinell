namespace Brinell.Maui.UITests;

/// <summary>
/// Marks a test that cannot run in background mode, and says which kind of exception it is.
/// </summary>
/// <remarks>
/// <para>
/// Background mode refuses every real mouse, keyboard, clipboard and foreground-window call in
/// the framework. Almost the whole suite runs under it — the point of stage B. A handful of tests
/// cannot, and step 15 requires that they be excluded <b>by declaration rather than by
/// omission</b>: a filter that names this trait is a list someone maintains, where a test quietly
/// left out of a run is a list nobody can see.
/// </para>
/// <para>
/// <b>Two values, because two different things are being said.</b> A test of the physical path
/// should be here forever; a test waiting on a verb that does not exist yet should not, and
/// mixing them would turn a work list into a permanent exemption.
/// </para>
/// <example>
/// <code>
/// $env:BRINELL_BACKGROUND_MODE = "1"
/// dotnet test testsnew\Brinell.Maui.UITests --filter "PhysicalInput!=Deliberate&amp;PhysicalInput!=Pending"
/// </code>
/// </example>
/// </remarks>
public static class PhysicalInputTrait
{
    /// <summary>The trait name. Used by the attribute and by the filter, so they cannot drift.</summary>
    public const string Name = "PhysicalInput";

    /// <summary>
    /// This test is <i>about</i> real input, and is expected to stay that way.
    /// </summary>
    /// <remarks>
    /// The fallback path is a real feature: it is what every platform without the automation
    /// bridge uses, and what an uninstrumented app under test relies on. Something has to
    /// exercise it, and that something cannot run in a mode that forbids it.
    /// </remarks>
    public const string Deliberate = "Deliberate";

    /// <summary>
    /// This test uses real input only because the verb that would replace it is not built yet.
    /// </summary>
    /// <remarks>
    /// A work item, not an exemption. Each use names the step that will remove it, and the
    /// marker comes off when that step lands.
    /// </remarks>
    public const string Pending = "Pending";

    /// <summary>
    /// This test fails in background mode for a reason nobody has established yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Distinct from <see cref="Pending"/> on purpose. <c>Pending</c> says "we know what will fix
    /// this and which step does it"; this says "we do not know why this fails". Filing the second
    /// under the first would make an open question look like scheduled work, and it would stop
    /// being asked.
    /// </para>
    /// <para>
    /// A test marked this way <b>still runs, and passes, in the default configuration</b> — the
    /// marker excludes it from background-mode runs only. Nothing here is a test that does not
    /// work; each is a test that does not work <i>yet</i> in one mode.
    /// </para>
    /// </remarks>
    public const string Unresolved = "Unresolved";
}

/// <summary>
/// A fact that needs real input, and skips itself - with the reason - where input is refused.
/// </summary>
/// <remarks>
/// <para>
/// <b>The trait alone left a plain run red.</b> <see cref="PhysicalInputTrait.Deliberate"/> lets a
/// filter exclude these tests, but a <c>dotnet test</c> with nothing but
/// <c>BRINELL_BACKGROUND_MODE=1</c> set ran them anyway and reported two failures that were not
/// failures: the policy refused the click, exactly as designed. Quiet is now the default for this
/// stack, so without this every plain run would report them. Five consecutive full runs came
/// back 265 passed and 2 failed, and those 2 were always these. A suite that is only green under
/// the right filter is a suite whose red nobody reads.
/// </para>
/// <para>
/// <b>Skipped, not hidden.</b> The skip is decided when the test is discovered and carries the
/// reason, so it shows in every run as a skip with a sentence - the same visibility the trait was
/// for, without the false failure.
/// </para>
/// </remarks>
public sealed class PhysicalInputFactAttribute : Xunit.FactAttribute
{
    public PhysicalInputFactAttribute()
    {
        // Discovery can run before any driver type is touched, and the quiet default is declared
        // by the FlaUI assembly's module initializer - so make sure it has run before asking.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(
            typeof(Brinell.Maui.FlaUI.FlaUIMauiDriver).Module.ModuleHandle);

        if (Brinell.Core.Diagnostics.PhysicalInput.Policy
            == Brinell.Core.Diagnostics.PhysicalInputPolicy.Refused)
        {
            Skip = "Exercises real input on purpose, and physical input is refused in this run "
                + "(BRINELL_BACKGROUND_MODE). Run without it to exercise the fallback path.";
        }
    }
}
