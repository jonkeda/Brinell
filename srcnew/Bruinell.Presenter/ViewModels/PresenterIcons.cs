namespace Bruinell.Presenter.ViewModels;

/// <summary>
/// The Segoe glyphs the Presenter draws, one name per meaning.
/// </summary>
internal static class PresenterIcons
{
    // Toolbar
    public const string OpenFolder = "";   // open folder
    public const string Reload = "";       // refresh
    public const string Validate = "";     // check
    public const string Run = "";          // play
    public const string Stop = "";         // stop
    public const string Next = "";         // step forward

    // The Speed button: a gauge, high to low.
    public const string SpeedFast = "";   // speed high
    public const string SpeedNormal = ""; // speed medium
    public const string SpeedSlow = "";   // speed off

    // Theme toggle. Names the destination, not the current state.
    public const string ThemeLight = "";   // sun
    public const string ThemeDark = "";    // moon

    // Tabs
    public const string TabTree = "";      // indented hierarchy
    public const string TabConfig = "";    // settings gear
    public const string TabDiagnostics = "";
    public const string TabDiscovery = ""; // search
    public const string TabCommandCatalog = ""; // bulleted list

    // Narrator mode
    public const string ModeNarrator = ""; // video camera
    public const string OpenScript = "";   // open file
    public const string Say = "";          // message: a spoken line

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
