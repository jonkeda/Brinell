namespace Brinell.Maui.Calls;

/// <summary>
/// The state of one phase of one call: its deadline, what each attempt saw, and the scroll
/// throttles.
/// </summary>
/// <remarks>
/// Control and scope objects are often created fresh on every property access
/// (<c>=&gt; new(this, "Id")</c>), so this state cannot live on them. It lives for one phase of one
/// call instead, which is also the right unit for "at most once per Animation interval".
/// </remarks>
internal sealed class AttemptContext
{
    private static readonly AsyncLocal<AttemptContext?> CurrentContext = new();

    private readonly int _animationMs;
    private readonly Dictionary<string, long> _scrolledAt = new(StringComparer.Ordinal);
    private long? _sweptAt;

    public AttemptContext(Deadline deadline, int animationMs)
    {
        Deadline = deadline;
        _animationMs = Math.Max(0, animationMs);
    }

    /// <summary>
    /// The context of the call running on this flow, or null outside any call. Set by
    /// <see cref="ControlCall"/> around its body, so code below a call - a Core method, a driver
    /// scroll - can be given what is left of the call's budget without a parameter for it.
    /// </summary>
    public static AttemptContext? Current
    {
        get => CurrentContext.Value;
        set => CurrentContext.Value = value;
    }

    /// <summary>
    /// What is left of the running call's budget, or <paramref name="outsideACallMs"/> when no
    /// call is running (a plain <c>Is*</c> member).
    /// </summary>
    public static int RemainingOr(int outsideACallMs) => Current?.Deadline.RemainingMs ?? outsideACallMs;

    /// <summary>The phase's deadline.</summary>
    public Deadline Deadline { get; }

    /// <summary>What each attempt saw.</summary>
    public ObservationLog Log { get; } = new();

    /// <summary>
    /// Whether a scroll-to-find may run now: the first time, then at most once per Animation
    /// interval (F3). A list that is still moving is given time to settle between sweeps.
    /// </summary>
    public bool MaySweep()
    {
        var now = Deadline.ElapsedMs;
        if (_sweptAt is { } last && now - last < _animationMs)
        {
            return false;
        }

        _sweptAt = now;
        return true;
    }

    /// <summary>
    /// Whether an element may be scrolled into view now: the first time for this element or
    /// locator (<paramref name="key"/>), then at most once per Animation interval (plan 3.1, S3).
    /// </summary>
    public bool MayScrollIntoView(string key)
    {
        var now = Deadline.ElapsedMs;
        if (_scrolledAt.TryGetValue(key, out var last) && now - last < _animationMs)
        {
            return false;
        }

        _scrolledAt[key] = now;
        return true;
    }
}
