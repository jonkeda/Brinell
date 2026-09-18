using Brinell.Maui.AppSupport.Uia;
using Brinell.Uia;

namespace Brinell.Samples.Todo.App.Pages;

/// <summary>
/// The gesture-bridge verbs every Todo page declares.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why pages declare anything.</b> On Windows a MAUI <c>ToolbarItem</c> renders into native
/// chrome, and invoking it through UI Automation reports success without raising its command -
/// measured in the sample app. Add, Sync, Edit, Delete, Save and Cancel are all toolbar items here,
/// so each page answers <c>InvokeToolbarItem</c> for its own, and the navigation questions a test
/// asks before it acts (where are we, is the app idle, is an alert up).
/// </para>
/// <para>
/// Nothing else in the app is on the bridge: buttons, entries, switches and the list all have UI
/// Automation patterns (AD-008, test 1). Inert in a build without the bridge.
/// </para>
/// </remarks>
internal static class PageAutomation
{
    private static readonly string Verbs = string.Join(
        ",",
        nameof(BrinellVerb.NavigateBack),
        nameof(BrinellVerb.GetState),
        nameof(BrinellVerb.CurrentRoute),
        nameof(BrinellVerb.IsIdle),
        nameof(BrinellVerb.CurrentAlert),
        nameof(BrinellVerb.InvokeMenuItem),
        nameof(BrinellVerb.InvokeToolbarItem));

    /// <summary>Declares the verbs on <paramref name="page"/>, whose AutomationId must already be set.</summary>
    public static void Declare(ContentPage page) => GestureAutomation.SetVerbs(page, Verbs);
}
