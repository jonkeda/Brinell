namespace Brinell.Presenter.ViewModels;

/// <summary>
/// The Segoe glyphs the Presenter draws, one name per meaning.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every codepoint here was verified, not looked up.</b> Each one was checked for presence in
/// both <c>SegoeIcons.ttf</c> (Segoe Fluent Icons, Windows 11) and <c>segmdl2.ttf</c> (Segoe MDL2
/// Assets, Windows 10 and 11), and then rendered and eyeballed to confirm the shape means what the
/// name says. All of them are in both fonts, which is what lets
/// <c>App.ResolveIconFont</c> fall back from one to the other without changing a value here.
/// </para>
/// <para>
/// <b>These never reach the hidden text mirrors.</b> <c>WorkspaceTreeText</c> and friends are what
/// the UAT suite asserts against, and they stay plain ASCII - see
/// <see cref="UatWorkspaceNodeViewModel.DisplayText"/>, which keeps using <c>Icon</c> and
/// <c>ExpansionText</c> while the on-screen row binds <c>IconGlyph</c> and <c>ExpansionGlyph</c>.
/// </para>
/// </remarks>
internal static class PresenterIcons
{
    // Toolbar
    public const string OpenFolder = "";   // open folder
    public const string Reload = "";       // refresh
    public const string Validate = "";     // check
    public const string Run = "";          // play
    public const string Stop = "";         // stop
    public const string Next = "";         // step forward

    // Theme toggle. Names the destination, not the current state.
    public const string ThemeLight = "";   // sun
    public const string ThemeDark = "";    // moon

    // Tabs
    public const string TabTree = "";      // indented hierarchy
    public const string TabConfig = "";    // settings gear
    public const string TabDiagnostics = "";
    public const string TabDiscovery = ""; // search
    public const string TabCommandCatalog = ""; // bulleted list

    // Chevrons
    public const string ChevronDown = "";
    public const string ChevronUp = "";
    public const string ChevronRight = "";

    // Workspace row: node kind
    public const string KindFolder = "";
    public const string KindMarkdown = "";
    public const string KindConfig = "";
    public const string KindSuite = "";
    public const string KindFile = "";

    // Workspace row: run status. Deliberately the same glyphs as the toolbar buttons that
    // cause them - a passed step wears the Validate check, a running one the Run play.
    public const string StatusPass = "";
    public const string StatusRun = "";
    public const string StatusFail = "";
    public const string StatusSkip = "";
    public const string StatusCancel = "";
    public const string StatusWait = "";
}
