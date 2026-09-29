using System.Text.Json;
using Brinell.Bdd.Preview;

namespace Brinell.Presenter.Services;

/// <summary>
/// The Scenario Preview the IDEs show, in a window of its own: the whole feature file, with the
/// selected scenario's run so far and the step the run is at.
/// </summary>
public interface IScenarioPreviewService
{
    /// <summary>Whether the window is open.</summary>
    bool IsOpen { get; }

    /// <summary>The page has loaded and can be shown a state; raised again after the window reopens.</summary>
    event Action? Ready;

    /// <summary>A message the page sent (<c>debug</c>, <c>reveal</c> ...), with its <c>type</c>.</summary>
    event Action<string, JsonElement>? Message;

    /// <summary>Opens the window, or brings it to the front.</summary>
    void Open();

    /// <summary>Shows a state; ignored while the window is closed.</summary>
    /// <param name="state">What to show.</param>
    void Show(ScenarioPreviewState state);
}

/// <summary>What the preview shows.</summary>
/// <param name="FilePath">The feature file.</param>
/// <param name="ScenarioName">The selected scenario, whose run is shown.</param>
/// <param name="ScenarioLine">Its heading line, scrolled to when no run is on.</param>
/// <param name="Run">The run so far; null before it starts.</param>
/// <param name="CurrentLine">The line of the step (or call) the run is at; null when no run is on.</param>
/// <param name="Catalogs">The suite's catalogs; null to parse only.</param>
/// <param name="Dark">Whether Presenter shows its dark theme.</param>
/// <param name="Suite">The suite's name, for the bind state.</param>
/// <param name="Paused">Where the run is paused, for the preview's bar; null while it runs or is off.</param>
public sealed record ScenarioPreviewState(
    string FilePath,
    string ScenarioName,
    int ScenarioLine,
    BddScenarioRun? Run,
    int? CurrentLine,
    BddCatalogSet? Catalogs,
    bool Dark,
    string Suite,
    ScenarioPreviewPause? Paused = null);

/// <summary>A pause, as the preview's bar shows it.</summary>
/// <param name="Text">The step, or the call.</param>
/// <param name="Line">Its line in the feature file.</param>
/// <param name="IsCall">Whether it is a <c>## Step:</c> call: Step into is offered.</param>
/// <param name="InCall">Whether it is inside one: Step out is offered.</param>
public sealed record ScenarioPreviewPause(string Text, int Line, bool IsCall, bool InCall);
