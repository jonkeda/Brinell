using System.Runtime.InteropServices;

namespace Brinell.Uia.Interop;

/// <summary>
/// The subset of <c>UIAutomationCore</c>'s type system the registrar needs.
/// </summary>
/// <remarks>
/// <para>
/// Hand-declared because it has to be. <c>Interop.UIAutomationClient</c>, the interop
/// assembly FlaUI embeds, contains the client-side automation interfaces and none of the
/// registration machinery - no <c>IUIAutomationRegistrar</c>, no
/// <c>IUIAutomationPatternHandler</c>, none of the descriptor structs. Every declaration in
/// this folder was checked against <c>uiautomationcore.h</c>; a wrong field order here
/// produces a corrupt read rather than an error, so treat changes as interop changes.
/// </para>
/// </remarks>
internal static class UiaTypes
{
    /// <summary>
    /// Every descriptor below is blittable on purpose.
    /// </summary>
    /// <remarks>
    /// UI Automation keeps the pointers it is handed at registration - the method table, and
    /// every name in it - for the life of the process. A <c>string</c> field marshalled by the
    /// runtime would be copied into a native buffer freed when the call returns, leaving UI
    /// Automation reading released memory at some later and entirely unrelated moment. So the
    /// names are <see cref="IntPtr"/> fields, allocated and deliberately never freed by
    /// <see cref="NativeTable"/>, and the structs cross the boundary as raw addresses.
    /// </remarks>
    internal const bool DescriptorsAreBlittable = true;
}

/// <summary>The type of one parameter or property in a custom pattern.</summary>
/// <remarks>
/// The flags matter: <c>Out</c> (0x20000) or'd into a base type gives the by-reference form,
/// which is how a method declares that a parameter is a result rather than an argument.
/// </remarks>
internal enum UIAutomationType
{
    Int = 0x1,
    Bool = 0x2,
    String = 0x3,
    Double = 0x4,
    Element = 0x7,

    Array = 0x10000,
    Out = 0x20000,

    OutInt = Out | Int,
    OutBool = Out | Bool,
    OutString = Out | String,
    OutDouble = Out | Double,
}

/// <summary>One argument in a dispatched call: a type tag and a pointer to the storage.</summary>
/// <remarks>
/// For an in-parameter, <see cref="Data"/> points at the value. For an out-parameter it points
/// at the caller's storage, which the provider writes. Getting that backwards writes over the
/// caller's stack, so both directions are handled in one place - see
/// <see cref="NativeParameters"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct UIAutomationParameter
{
    public UIAutomationType Type;
    public IntPtr Data;
}

/// <summary>Describes one method in a custom pattern's frozen method table.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UIAutomationMethodInfo
{
    /// <summary><c>LPCWSTR</c>, owned for the life of the process.</summary>
    public IntPtr ProgrammaticName;

    /// <summary>Whether UI Automation should focus the element before dispatching.</summary>
    /// <remarks>
    /// Always zero. Focus is a verb the caller asks for explicitly
    /// (<see cref="BrinellVerb.Focus"/>); letting UI Automation take it as a side effect of
    /// every call would defeat the entire point of the bridge, which is to act on an app that
    /// is not in the foreground.
    /// </remarks>
    public int DoSetFocus;

    public uint InParameterCount;
    public uint OutParameterCount;

    /// <summary><c>UIAutomationType*</c>, in declaration order, in then out.</summary>
    public IntPtr ParameterTypes;

    /// <summary><c>LPCWSTR*</c>, parallel to <see cref="ParameterTypes"/>.</summary>
    public IntPtr ParameterNames;
}

/// <summary>Describes one custom property. Unused: the contract registers no properties.</summary>
/// <remarks>
/// Kept declared because <see cref="UIAutomationPatternInfo"/> has a field of this type and a
/// reader should be able to see what the null pointer stands for.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct UIAutomationPropertyInfo
{
    public Guid Guid;
    public IntPtr ProgrammaticName;
    public UIAutomationType Type;
}

/// <summary>Describes one custom event. Unused, for the same reason.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UIAutomationEventInfo
{
    public Guid Guid;
    public IntPtr ProgrammaticName;
}

/// <summary>The complete description handed to <c>IUIAutomationRegistrar.RegisterPattern</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UIAutomationPatternInfo
{
    public Guid Guid;

    /// <summary><c>LPCWSTR</c>, owned for the life of the process.</summary>
    public IntPtr ProgrammaticName;

    public Guid ProviderInterfaceId;
    public Guid ClientInterfaceId;

    public uint PropertyCount;
    public IntPtr Properties;

    public uint MethodCount;

    /// <summary><c>UIAutomationMethodInfo*</c>. This array is the frozen method table.</summary>
    public IntPtr Methods;

    public uint EventCount;
    public IntPtr Events;

    /// <summary>
    /// <c>IUIAutomationPatternHandler*</c>. UI Automation calls back into this to build the
    /// client wrapper and to dispatch calls to the provider.
    /// </summary>
    public IntPtr PatternHandler;
}

/// <summary>Which way <c>IRawElementProviderFragment.Navigate</c> is walking.</summary>
internal enum NavigateDirection
{
    Parent = 0,
    NextSibling = 1,
    PreviousSibling = 2,
    FirstChild = 3,
    LastChild = 4,
}

/// <summary>How UI Automation should treat a provider.</summary>
[Flags]
internal enum ProviderOptions
{
    ClientSideProvider = 0x1,
    ServerSideProvider = 0x2,
    NonClientAreaProvider = 0x4,
    OverrideProvider = 0x8,
    ProviderOwnsSetFocus = 0x10,
    UseComThreading = 0x20,
    RefuseNonClientSupport = 0x40,
    HasNativeIAccessible = 0x80,
    UseClientCoordinates = 0x100,
}

/// <summary>A rectangle in screen coordinates, as UI Automation expresses it.</summary>
/// <remarks>Width and height, not right and bottom. Easy to get wrong and silent when wrong.</remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct UiaRect
{
    public double Left;
    public double Top;
    public double Width;
    public double Height;
}

/// <summary>What changed, for <c>UiaRaiseStructureChangedEvent</c>.</summary>
internal enum StructureChangeType
{
    ChildAdded = 0,
    ChildRemoved = 1,
    ChildrenInvalidated = 2,
    ChildrenBulkAdded = 3,
    ChildrenBulkRemoved = 4,
    ChildrenReordered = 5,
}

/// <summary>The well-known UI Automation property and control-type ids this bridge answers.</summary>
/// <remarks>
/// Literal values rather than a reference to an interop assembly: this file compiles into the
/// app under test, which must not acquire a dependency to carry the bridge. The numbers are
/// part of the UI Automation ABI and have not changed since Windows 7.
/// </remarks>
internal static class UiaPropertyIds
{
    internal const int ControlType = 30003;
    internal const int Name = 30005;
    internal const int IsKeyboardFocusable = 30009;
    internal const int IsEnabled = 30010;
    internal const int AutomationId = 30011;
    internal const int ClassName = 30012;
    internal const int HelpText = 30013;
    internal const int IsControlElement = 30016;
    internal const int IsContentElement = 30017;
    internal const int IsOffscreen = 30022;
    internal const int FrameworkId = 30024;
    internal const int ProviderDescription = 30107;
}

/// <summary>The control types the bridge reports.</summary>
internal static class UiaControlTypeIds
{
    internal const int Custom = 50025;
    internal const int Pane = 50033;
}
