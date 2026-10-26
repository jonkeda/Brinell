using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Bruinet.Bdd.VisualStudio;

/// <summary>Tools, Options, Bruinet BDD, General: what Record uses when a video script does not say, and the warm runner.</summary>
[ComVisible(true)]
[Guid("7d7c1f2e-3a51-4b8e-9d0f-2b6f7a0c4e11")]
public sealed class BddOptionsPage : DialogPage
{
    /// <summary>Where Record puts its videos; empty for the suite's TestResults\recordings.</summary>
    [Category("Recording")]
    [DisplayName("Recordings folder")]
    [Description("Where Record puts its videos. Empty: TestResults\\recordings of the suite's results.")]
    public string RecordingsFolder { get; set; } = string.Empty;

    /// <summary>The voice, by (part of) its name, or <c>azure:&lt;voice&gt;</c>; empty for the default.</summary>
    [Category("Recording")]
    [DisplayName("Voice")]
    [Description("The voice a recording speaks with, by (part of) its name, when the video script names none: a Windows voice, or a cloud voice as azure:<voice> (azure:coral; its key in voice.secrets.json). Empty: the cloud voice when one is set up, else the system's default voice.")]
    public string Voice { get; set; } = string.Empty;

    /// <summary>The wait after a step, in milliseconds.</summary>
    [Category("Recording")]
    [DisplayName("Beat (ms)")]
    [Description("How long a recording waits after a step, in milliseconds, when the video script does not say.")]
    public int Beat { get; set; } = 600;

    /// <summary>The touch marks' accent, <c>#RRGGBB</c>; empty for Exact red.</summary>
    [Category("Recording")]
    [DisplayName("Touch colour")]
    [Description("The colour of a recording's touch marks, as #RRGGBB, when the video script does not say (TouchColor). Empty: Exact red, #E8111E.")]
    public string TouchColor { get; set; } = string.Empty;

    /// <summary>Whether a suite's runner is kept ready (AD-025); read when the language server starts.</summary>
    [Category("Runs")]
    [DisplayName("Warm runner")]
    [Description("Start a suite's runner, from a copy of its build output, when one of its feature files opens and after each build, so Run and Step start at once. False: each run starts its own. Takes effect when the solution is opened again.")]
    public bool WarmRunner { get; set; } = true;

    /// <summary>How long a warm runner waits after its suite's last feature file closes.</summary>
    [Category("Runs")]
    [DisplayName("Warm runner idle minutes")]
    [Description("How long a suite's warm runner waits after its last feature file closes before it ends. Takes effect when the solution is opened again.")]
    public double WarmRunnerIdleMinutes { get; set; } = 10;
}
