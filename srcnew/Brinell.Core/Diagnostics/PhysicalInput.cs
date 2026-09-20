using System.Collections.Concurrent;
using Brinell.Core.Exceptions;

namespace Brinell.Core.Diagnostics;

/// <summary>
/// What should happen when the framework is about to use real mouse, keyboard or clipboard.
/// </summary>
public enum PhysicalInputPolicy
{
    /// <summary>Physical input is performed. The default.</summary>
    Allowed,

    /// <summary>Physical input is performed, and every use is recorded.</summary>
    Audited,

    /// <summary>Physical input throws instead of happening.</summary>
    Refused,
}

/// <summary>
/// The gate every real mouse, keyboard or clipboard action passes through.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> Physical input is the reason a UI test run owns the machine: the
/// cursor, the foreground window, the keyboard focus queue and the clipboard are all
/// desktop-global and shared with whoever is sitting at the keyboard. Everything Brinell does
/// through a UI Automation pattern is invisible to them; everything it does through real input
/// is not.
/// </para>
/// <para>
/// <b>Two modes, for two jobs.</b> <see cref="PhysicalInputPolicy.Audited"/> is a measuring
/// instrument: the suite behaves exactly as before and every use is recorded, so one run yields
/// a complete list of the paths that would have to change. <see cref="PhysicalInputPolicy.Refused"/>
/// is the enforcement: once semantic routes exist, this is what stops a quiet regression back to
/// the mouse. Auditing first matters — refusing aborts each test at its first offending call, so
/// it reports one finding per test rather than all of them.
/// </para>
/// <para>
/// <b>Resolution, in order.</b> An <see cref="OverridePolicy"/> scope wins. Then
/// <c>BRINELL_BACKGROUND_MODE</c> if it is set at all: <c>audit</c> audits; <c>0</c>, <c>false</c>,
/// <c>off</c> or <c>allow</c> allow; anything else refuses. When it is not set,
/// <see cref="PhysicalInputPolicy.Allowed"/>. Point <c>BRINELL_PHYSICAL_INPUT_LOG</c> at a file to
/// have audited uses appended to it.
/// </para>
/// <para>
/// <b>Who still passes through this gate.</b> The WPF and WinForms FlaUI drivers, which fall back
/// to real input with no verb layer behind them - refusing it by default would fail their suites
/// rather than quieten them. The MAUI FlaUI driver used to as well, and made refusal its default;
/// it now has no physical input to guard, so it neither calls this nor sets a default
/// (see <c>.docs/decisions/ad-005-physical-input-is-opt-in.md</c>).
/// </para>
/// </remarks>
public static class PhysicalInput
{
    private static readonly Lazy<PhysicalInputPolicy?> ExplicitPolicy = new(ReadExplicitPolicy);

    private static readonly Lazy<string?> LogPath = new(
        () => Environment.GetEnvironmentVariable("BRINELL_PHYSICAL_INPUT_LOG"));

    private static readonly ConcurrentDictionary<string, PhysicalInputUse> Recorded = new();
    private static readonly object LogGate = new();

    private static readonly AsyncLocal<PhysicalInputPolicy?> Override = new();

    /// <summary>The policy in force, an active <see cref="OverridePolicy"/> scope winning.</summary>
    public static PhysicalInputPolicy Policy
        => Override.Value
           ?? ExplicitPolicy.Value
           ?? PhysicalInputPolicy.Allowed;

    /// <summary>
    /// Applies a policy for the duration of the returned scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The environment is read once per process, deliberately: a policy that changed mid-run would
    /// make an audit meaningless. That also makes the interesting behaviour — recording, and
    /// refusing — impossible to test, which is what this exists for.
    /// </para>
    /// <para>
    /// Scoped with <see cref="AsyncLocal{T}"/> rather than a plain static so that test classes
    /// running in parallel cannot see each other's override.
    /// </para>
    /// </remarks>
    /// <param name="policy">The policy to apply within the scope.</param>
    /// <returns>A scope that restores the previous policy when disposed.</returns>
    public static IDisposable OverridePolicy(PhysicalInputPolicy policy)
        => new PolicyScope(policy);

    private sealed class PolicyScope : IDisposable
    {
        private readonly PhysicalInputPolicy? _previous;

        internal PolicyScope(PhysicalInputPolicy policy)
        {
            _previous = Override.Value;
            Override.Value = policy;
        }

        public void Dispose() => Override.Value = _previous;
    }

    /// <summary>Every distinct call site recorded so far, keyed by its name.</summary>
    public static IReadOnlyDictionary<string, PhysicalInputUse> Uses => Recorded;

    /// <summary>
    /// Declares that the caller is about to use real input, and applies the policy.
    /// </summary>
    /// <param name="callSite">
    /// What is about to happen, named so it can be found in the source — for example
    /// <c>FlaUIWpfElement.RightClick</c>.
    /// </param>
    /// <param name="replacement">
    /// The semantic route that should replace it, so the inventory doubles as a work list.
    /// </param>
    /// <exception cref="PhysicalInputRefusedException">
    /// Thrown when the policy is <see cref="PhysicalInputPolicy.Refused"/>.
    /// </exception>
    public static void Used(string callSite, string replacement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callSite);
        ArgumentException.ThrowIfNullOrWhiteSpace(replacement);

        var policy = Policy;
        if (policy == PhysicalInputPolicy.Allowed)
        {
            return;
        }

        Recorded.AddOrUpdate(
            callSite,
            _ => new PhysicalInputUse(callSite, replacement, 1),
            (_, existing) => existing with { Count = existing.Count + 1 });

        if (policy == PhysicalInputPolicy.Refused)
        {
            throw new PhysicalInputRefusedException(callSite, replacement);
        }

        Append(callSite, replacement);
    }

    /// <summary>Forgets everything recorded. For tests of this type itself.</summary>
    public static void ResetRecorded() => Recorded.Clear();

    private static PhysicalInputPolicy? ReadExplicitPolicy()
        => Environment.GetEnvironmentVariable("BRINELL_BACKGROUND_MODE")?.Trim().ToLowerInvariant() switch
        {
            null or "" => null,
            "0" or "false" or "off" or "allow" or "allowed" => PhysicalInputPolicy.Allowed,
            "audit" => PhysicalInputPolicy.Audited,
            _ => PhysicalInputPolicy.Refused,
        };

    private static void Append(string callSite, string replacement)
    {
        var path = LogPath.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            lock (LogGate)
            {
                File.AppendAllText(path, $"{callSite}\t{replacement}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
            // A diagnostic that cannot write must not take the run down with it.
        }
        catch (UnauthorizedAccessException)
        {
            // Same.
        }
    }
}

/// <summary>One physical-input call site, and how often it was reached.</summary>
/// <param name="CallSite">Where the input happens.</param>
/// <param name="Replacement">The semantic route that should replace it.</param>
/// <param name="Count">How many times it was reached.</param>
public sealed record PhysicalInputUse(string CallSite, string Replacement, int Count);
