using System.Globalization;
using Brinell.Uia;
using Brinell.Uia.Provider;

namespace Brinell.Uia.TestHost;

/// <summary>
/// A verb target that remembers what it was asked to do and will read it back.
/// </summary>
/// <remarks>
/// <para>
/// The point of the spike is to prove that arguments survive the trip into another process
/// and that results survive the trip back. A target that only returned success would prove
/// the call happened and nothing about what arrived, so this one echoes: every
/// <see cref="Invoke"/> is appended to a log the client can read back with
/// <see cref="BrinellVerb.GetState"/>, which makes an argument that arrives mangled visible
/// rather than merely successful.
/// </para>
/// <para>
/// Also the negative case. <see cref="BrinellVerb.SwipeUp"/> is deliberately absent from
/// <see cref="Capabilities"/>, so a client asking for it exercises the "this element does not
/// do that" path rather than a crash.
/// </para>
/// </remarks>
internal sealed class RecordingTarget : IBrinellVerbTarget
{
    private readonly List<string> _log = [];
    private readonly Lock _gate = new();

    private string _text = string.Empty;

    internal RecordingTarget(string automationId) => AutomationId = automationId;

    public string AutomationId { get; }

    public IReadOnlyCollection<BrinellVerb> Capabilities { get; } =
    [
        BrinellVerb.Tap,
        BrinellVerb.DoubleTap,
        BrinellVerb.SwipeLeft,
        BrinellVerb.Pan,
        BrinellVerb.Focus,
        BrinellVerb.SetText,
        BrinellVerb.GetText,
        BrinellVerb.GetState,
    ];

    /// <summary>Always. The spike has nothing that goes away; step 28 tests that separately.</summary>
    public bool IsAvailable => true;

    public int Invoke(BrinellVerb verb, int arg1, int arg2)
    {
        lock (_gate)
        {
            _log.Add(string.Create(
                CultureInfo.InvariantCulture, $"{(int)verb}:{arg1}:{arg2}"));
        }

        return HResults.S_OK;
    }

    public int Exchange(BrinellVerb verb, string argument, out string result)
    {
        result = string.Empty;

        switch (verb)
        {
            case BrinellVerb.SetText:
                lock (_gate)
                {
                    _text = argument;
                }

                return HResults.S_OK;

            case BrinellVerb.GetText:
                lock (_gate)
                {
                    result = _text;
                }

                return HResults.S_OK;

            case BrinellVerb.GetState:
                lock (_gate)
                {
                    result = argument switch
                    {
                        "log" => string.Join("|", _log),
                        "count" => _log.Count.ToString(CultureInfo.InvariantCulture),
                        _ => string.Empty,
                    };
                }

                return HResults.S_OK;

            default:
                return HResults.UIA_E_NOTSUPPORTED;
        }
    }

    /// <summary>A fixed rectangle, so the bounds path is exercised with a checkable value.</summary>
    public bool TryGetScreenBounds(out BrinellBounds bounds)
    {
        bounds = new BrinellBounds(11, 22, 33, 44);
        return true;
    }
}
