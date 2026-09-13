using System.Runtime.CompilerServices;
using Brinell.Core.Diagnostics;

namespace Brinell.Maui.FlaUI;

/// <summary>
/// Makes a MAUI run through FlaUI quiet unless somebody asks otherwise.
/// </summary>
/// <remarks>
/// <para>
/// <b>The end state of the quiet-run plan, as a default rather than a setting.</b> A run that never
/// takes the keyboard, the pointer or the foreground used to need
/// <c>BRINELL_BACKGROUND_MODE=1</c>; a framework whose default takes the machine is one people run
/// less often. Every path this driver needs now has a semantic route, measured over consecutive
/// full runs, so silence means quiet.
/// </para>
/// <para>
/// <b>A module initializer, so it is in force before anything asks.</b> Test discovery reads the
/// policy - <c>PhysicalInputFact</c> decides its skip then - and that can happen before any driver
/// is constructed. <c>BRINELL_BACKGROUND_MODE=0</c> still allows real input, for the tests of the
/// fallback path and for anyone who wants to watch.
/// </para>
/// </remarks>
internal static class QuietByDefault
{
    // CA2255 warns that a library module initializer changes process state as a side effect of
    // loading. That is the point here, and it is safe for the reason the rule worries about: the
    // change is one-way, idempotent, and only decides what an unset variable means.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Declare() => PhysicalInput.QuietByDefault();
}
