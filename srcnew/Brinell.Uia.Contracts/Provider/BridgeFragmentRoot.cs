using System.Runtime.InteropServices;
using Brinell.Uia.Interop;

namespace Brinell.Uia.Provider;

/// <summary>
/// The root of the bridge's fragment: one element per instrumented target, hanging off the
/// bridge window.
/// </summary>
/// <remarks>
/// <para>
/// Carries no pattern itself. It exists to be somewhere for the target elements to hang, and
/// to be the thing <c>WM_GETOBJECT</c> returns.
/// </para>
/// <para>
/// <b>The child list is swapped, never mutated.</b> UI Automation walks the fragment from
/// arbitrary threads and will interleave a walk with a registration; handing every walk an
/// immutable snapshot means a walk in progress sees a consistent tree even when it is one
/// revision out of date.
/// </para>
/// </remarks>
[ComVisible(true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class BridgeFragmentRoot
    : IRawElementProviderSimple, IRawElementProviderFragment, IRawElementProviderFragmentRoot
{
    private readonly IntPtr _hwnd;
    private readonly Lock _gate = new();

    private BridgeTargetProvider[] _children = [];

    internal BridgeFragmentRoot(IntPtr hwnd) => _hwnd = hwnd;

    internal BridgeTargetProvider[] Children => Volatile.Read(ref _children);

    internal void SetChildren(BridgeTargetProvider[] children)
    {
        lock (_gate)
        {
            Volatile.Write(ref _children, children);
        }
    }

    // ---- IRawElementProviderSimple -------------------------------------------------------

    public ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider;

    /// <summary>The root carries no patterns. Only the target elements do.</summary>
    public object? GetPatternProvider(int patternId) => null;

    public object? GetPropertyValue(int propertyId) => propertyId switch
    {
        UiaPropertyIds.AutomationId => BrinellUiaIds.BridgeAutomationId,
        UiaPropertyIds.ClassName => BrinellUiaIds.BridgeClassName,
        UiaPropertyIds.ControlType => UiaControlTypeIds.Pane,
        UiaPropertyIds.ProviderDescription => BrinellUiaIds.ProviderDescription,
        UiaPropertyIds.FrameworkId => "Brinell",

        // Raw view only. Narrator, Voice Access and Magnifier walk the control and content
        // views, so none of them ever sees the bridge; FlaUI's descendant search walks raw and
        // does. This is what keeps a test hook out of the accessibility tree.
        UiaPropertyIds.IsControlElement => false,
        UiaPropertyIds.IsContentElement => false,

        UiaPropertyIds.IsKeyboardFocusable => false,

        _ => null,
    };

    /// <summary>
    /// The default provider for the bridge window.
    /// </summary>
    /// <remarks>
    /// A fragment root must return this. It is what joins the fragment to the rest of the
    /// desktop tree; without it the fragment exists but nothing can navigate into it.
    /// </remarks>
    public IRawElementProviderSimple? HostRawElementProvider
        => UiaNativeMethods.UiaHostProviderFromHwnd(_hwnd);

    // ---- IRawElementProviderFragment -----------------------------------------------------

    public object? Navigate(NavigateDirection direction)
    {
        var children = Children;

        return direction switch
        {
            NavigateDirection.FirstChild => children.Length > 0 ? children[0] : null,
            NavigateDirection.LastChild => children.Length > 0 ? children[^1] : null,

            // Parent and siblings are the host provider's business: this fragment root is the
            // window, and the window's place in the tree is not ours to describe.
            _ => null,
        };
    }

    /// <summary>Null: the host provider supplies the window's runtime id.</summary>
    public int[]? GetRuntimeId() => null;

    /// <summary>Empty: the host provider supplies the window's bounds.</summary>
    public UiaRect BoundingRectangle => default;

    public object[]? GetEmbeddedFragmentRoots() => null;

    /// <summary>
    /// Does nothing.
    /// </summary>
    /// <remarks>
    /// The bridge window is created disabled and non-activating precisely so it can never take
    /// focus. Focusing a target is a verb (<see cref="BrinellVerb.Focus"/>), which acts on the
    /// real element rather than on this.
    /// </remarks>
    public void SetFocus()
    {
    }

    public IRawElementProviderFragmentRoot? FragmentRoot => this;

    // ---- IRawElementProviderFragmentRoot -------------------------------------------------

    /// <summary>
    /// Null.
    /// </summary>
    /// <remarks>
    /// Hit-testing must not resolve to a bridge element. The bridge window is one pixel and
    /// transparent to input; a point on screen belongs to the app's real UI, and answering
    /// otherwise would put instrumentation in front of the thing being tested.
    /// </remarks>
    public object? ElementProviderFromPoint(double x, double y) => null;

    /// <summary>Null: the bridge never holds focus.</summary>
    public object? GetFocus() => null;
}
