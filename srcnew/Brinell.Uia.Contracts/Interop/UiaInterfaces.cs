using System.Runtime.InteropServices;

namespace Brinell.Uia.Interop;

/*
 * Every IID below was read off the machine, not transcribed from a header or from memory.
 * Two of the seven were wrong on the first attempt - the registrar's and the pattern
 * instance's, both wrong only in their last six bytes - and the symptom was a bare
 * E_NOINTERFACE with nothing to say which declaration was at fault.
 *
 * To check them again:
 *
 *   Get-ChildItem 'HKLM:\SOFTWARE\Classes\Interface' | ForEach-Object {
 *     $v = (Get-ItemProperty $_.PSPath).'(default)'
 *     if ($v -like 'IUIAutomation*' -or $v -like 'IRawElementProvider*') { "$v $($_.PSChildName)" }
 *   }
 */

/// <summary>
/// Registers custom properties, events and patterns with UI Automation.
/// </summary>
[ComImport]
[Guid("8609c4ec-4a1a-4d88-a357-5a66e060e1cf")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationRegistrar
{
    [PreserveSig]
    int RegisterProperty(ref UIAutomationPropertyInfo property, out int propertyId);

    [PreserveSig]
    int RegisterEvent(ref UIAutomationEventInfo eventInfo, out int eventId);

    /// <param name="pattern">The pattern description, including its frozen method table.</param>
    /// <param name="patternId">The process-local id to answer <c>GetPatternProvider</c> with.</param>
    /// <param name="patternAvailablePropertyId">
    /// The id of the generated <c>IsXxxPatternAvailable</c> property. This is how a client asks
    /// whether an element carries the pattern without calling into it.
    /// </param>
    /// <param name="propertyIdCount">Zero: the contract registers no custom properties.</param>
    /// <param name="propertyIds">Null, for the same reason.</param>
    /// <param name="eventIdCount">Zero: the contract registers no custom events.</param>
    /// <param name="eventIds">Null, for the same reason.</param>
    [PreserveSig]
    int RegisterPattern(
        ref UIAutomationPatternInfo pattern,
        out int patternId,
        out int patternAvailablePropertyId,
        uint propertyIdCount,
        [Out] int[]? propertyIds,
        uint eventIdCount,
        [Out] int[]? eventIds);
}

/// <summary>
/// The client's handle on one element's instance of a custom pattern.
/// </summary>
[ComImport]
[Guid("c03a7fe4-9431-409f-bed8-ae7c2299bc8d")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationPatternInstance
{
    [PreserveSig]
    int GetProperty(uint index, [MarshalAs(UnmanagedType.Bool)] bool cached, UIAutomationType type, IntPtr value);

    /// <param name="index">Index into the frozen method table. Never a name - see the ids file.</param>
    /// <param name="parameters">A <c>UIAutomationParameter*</c> array.</param>
    /// <param name="parameterCount">Its length, in and out parameters together.</param>
    [PreserveSig]
    int CallMethod(uint index, IntPtr parameters, uint parameterCount);
}

/// <summary>
/// The two callbacks UI Automation needs to operate a custom pattern.
/// </summary>
[ComImport]
[Guid("d97022f3-a947-465e-8b2a-ac4315fa54e8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUIAutomationPatternHandler
{
    [PreserveSig]
    int CreateClientWrapper(
        IUIAutomationPatternInstance patternInstance,
        [MarshalAs(UnmanagedType.IUnknown)] out object? clientWrapper);

    [PreserveSig]
    int Dispatch(
        [MarshalAs(UnmanagedType.IUnknown)] object target,
        uint index,
        IntPtr parameters,
        uint parameterCount);
}

/// <summary>The minimum a UI Automation provider must implement.</summary>
[ComImport]
[Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IRawElementProviderSimple
{
    ProviderOptions ProviderOptions { get; }

    /// <param name="patternId">
    /// A process-local id. Compared against the id this process got back from the registrar,
    /// never against a constant.
    /// </param>
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object? GetPatternProvider(int patternId);

    [return: MarshalAs(UnmanagedType.Struct)]
    object? GetPropertyValue(int propertyId);

    /// <summary>
    /// The default provider for the window this one lives on.
    /// </summary>
    /// <remarks>
    /// A fragment root must return the host provider from <c>UiaHostProviderFromHwnd</c>, which
    /// is what attaches the fragment to the rest of the desktop tree. Elements below the root
    /// return null.
    /// </remarks>
    IRawElementProviderSimple? HostRawElementProvider { get; }
}

/// <summary>An element inside a provider-defined fragment.</summary>
[ComImport]
[Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IRawElementProviderFragment
{
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object? Navigate(NavigateDirection direction);

    /// <summary>
    /// Identifies the element across queries.
    /// </summary>
    /// <remarks>
    /// Must begin with <c>3</c> (<c>UiaAppendRuntimeId</c>) so UI Automation prefixes the host
    /// window's id. Without it, two apps running the bridge produce colliding runtime ids and
    /// clients cache the wrong element.
    /// </remarks>
    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)]
    int[]? GetRuntimeId();

    UiaRect BoundingRectangle { get; }

    [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
    object[]? GetEmbeddedFragmentRoots();

    void SetFocus();

    IRawElementProviderFragmentRoot? FragmentRoot { get; }
}

/// <summary>The root of a provider-defined fragment.</summary>
[ComImport]
[Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IRawElementProviderFragmentRoot
{
    [return: MarshalAs(UnmanagedType.IUnknown)]
    object? ElementProviderFromPoint(double x, double y);

    [return: MarshalAs(UnmanagedType.IUnknown)]
    object? GetFocus();
}

/// <summary>The <c>UIAutomationCore</c> entry points the bridge calls.</summary>
internal static class UiaNativeMethods
{
    private const string UiaCore = "UIAutomationCore.dll";

    /// <summary>
    /// Answers <c>WM_GETOBJECT</c> with a provider.
    /// </summary>
    [DllImport(UiaCore, ExactSpelling = true)]
    internal static extern IntPtr UiaReturnRawElementProvider(
        IntPtr hwnd,
        IntPtr wParam,
        IntPtr lParam,
        IRawElementProviderSimple provider);

    /// <summary>Builds the default provider for a window.</summary>
    [DllImport(UiaCore, ExactSpelling = true, PreserveSig = false)]
    [return: MarshalAs(UnmanagedType.Interface)]
    internal static extern IRawElementProviderSimple UiaHostProviderFromHwnd(IntPtr hwnd);

    /// <summary>
    /// Tells UI Automation to forget a provider before it is destroyed.
    /// </summary>
    [DllImport(UiaCore, ExactSpelling = true)]
    internal static extern int UiaDisconnectProvider(IRawElementProviderSimple provider);

    /// <summary>Announces that the fragment's children changed.</summary>
    [DllImport(UiaCore, ExactSpelling = true)]
    internal static extern int UiaRaiseStructureChangedEvent(
        IRawElementProviderSimple provider,
        StructureChangeType changeType,
        int[]? runtimeId,
        int runtimeIdLength);

    /// <summary>
    /// Whether any UI Automation client is connected.
    /// </summary>
    [DllImport(UiaCore, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UiaClientsAreListening();
}
