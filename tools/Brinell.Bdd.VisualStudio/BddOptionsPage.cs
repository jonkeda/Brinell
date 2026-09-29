using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Brinell.Bdd.VisualStudio;

/// <summary>Tools, Options, Brinell BDD, Recording: what Record uses when a video script does not say.</summary>
[ComVisible(true)]
[Guid("7d7c1f2e-3a51-4b8e-9d0f-2b6f7a0c4e11")]
public sealed class BddOptionsPage : DialogPage
{
    /// <summary>Where Record puts its videos; empty for the suite's TestResults\recordings.</summary>
    [Category("Recording")]
    [DisplayName("Recordings folder")]
    [Description("Where Record puts its videos. Empty: TestResults\\recordings of the suite's results.")]
    public string RecordingsFolder { get; set; } = string.Empty;

    /// <summary>The voice, by (part of) its name; empty for the system's default.</summary>
    [Category("Recording")]
    [DisplayName("Voice")]
    [Description("The voice a recording speaks with, by (part of) its name, when the video script names none. Empty: the system's default voice.")]
    public string Voice { get; set; } = string.Empty;

    /// <summary>The wait after a step, in milliseconds.</summary>
    [Category("Recording")]
    [DisplayName("Beat (ms)")]
    [Description("How long a recording waits after a step, in milliseconds, when the video script does not say.")]
    public int Beat { get; set; } = 600;
}
