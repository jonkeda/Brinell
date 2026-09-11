using System.Runtime.InteropServices;

namespace Brinell.Uia.Interop;

/// <summary>
/// Builds the frozen method table in unmanaged memory and keeps it there.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here is ever freed, on purpose.</b> UI Automation reads the method table long
/// after <c>RegisterPattern</c> returns, and registration happens once per process, so the
/// allocation is bounded and its lifetime is the process. Adding cleanup would introduce a
/// use-after-free with no upside; that is a decision, not an oversight.
/// </para>
/// <para>
/// <b>The order of the methods is the contract.</b> Index 0 is <c>Invoke</c>, index 1 is
/// <c>Exchange</c>, and UI Automation dispatches by that index alone.
/// <c>BrinellAutomationPatternHandler.Dispatch</c> switches on the same numbers; the two are
/// pinned together by <c>MethodTableMatchesDispatchTests</c>.
/// </para>
/// </remarks>
internal static class NativeTable
{
    /// <summary>Index of <see cref="IBrinellAutomationProvider.Invoke"/> in the method table.</summary>
    internal const uint InvokeMethodIndex = 0;

    /// <summary>Index of <see cref="IBrinellAutomationProvider.Exchange"/> in the method table.</summary>
    internal const uint ExchangeMethodIndex = 1;

    /// <summary>How many methods the table has. Frozen.</summary>
    internal const int MethodCount = 2;

    /// <summary>
    /// Describes the pattern, ready to hand to the registrar.
    /// </summary>
    /// <param name="patternHandler">
    /// A COM pointer to the handler. Caller keeps the reference alive for the process lifetime.
    /// </param>
    /// <returns>A blittable descriptor whose pointers stay valid forever.</returns>
    internal static UIAutomationPatternInfo BuildPatternInfo(IntPtr patternHandler)
    {
        return new UIAutomationPatternInfo
        {
            Guid = BrinellUiaIds.PatternGuid,
            ProgrammaticName = AllocString(BrinellUiaIds.PatternProgrammaticName),
            ProviderInterfaceId = new Guid(BrinellUiaIds.ProviderInterfaceIid),
            ClientInterfaceId = new Guid(BrinellUiaIds.ClientInterfaceIid),

            // No custom properties and no custom events.
            //
            // Both are supported by the registrar and both were considered. Properties would
            // let a client read an element's capabilities through a cache request rather than
            // a call, which matters when you are reading one property off a thousand elements.
            // The bridge is never asked about more than a handful, so the saving is nil and the
            // cost is real: each property adds a descriptor, a type tag and a marshalling path
            // that fails silently when it is wrong. Capabilities are read with
            // BrinellVerb.GetCapabilities instead.
            PropertyCount = 0,
            Properties = IntPtr.Zero,
            EventCount = 0,
            Events = IntPtr.Zero,

            MethodCount = MethodCount,
            Methods = AllocMethodTable(),

            PatternHandler = patternHandler,
        };
    }

    private static IntPtr AllocMethodTable()
    {
        var methods = new UIAutomationMethodInfo[MethodCount];

        // Index 0: Invoke(int verb, int arg1, int arg2)
        methods[InvokeMethodIndex] = BuildMethod(
            name: "Invoke",
            inTypes: [UIAutomationType.Int, UIAutomationType.Int, UIAutomationType.Int],
            outTypes: [],
            parameterNames: ["verb", "arg1", "arg2"]);

        // Index 1: Exchange(int verb, string argument, out string result)
        methods[ExchangeMethodIndex] = BuildMethod(
            name: "Exchange",
            inTypes: [UIAutomationType.Int, UIAutomationType.String],
            outTypes: [UIAutomationType.OutString],
            parameterNames: ["verb", "argument", "result"]);

        var size = Marshal.SizeOf<UIAutomationMethodInfo>();
        var block = Marshal.AllocHGlobal(size * MethodCount);

        for (var i = 0; i < MethodCount; i++)
        {
            Marshal.StructureToPtr(methods[i], block + (size * i), fDeleteOld: false);
        }

        return block;
    }

    private static UIAutomationMethodInfo BuildMethod(
        string name,
        UIAutomationType[] inTypes,
        UIAutomationType[] outTypes,
        string[] parameterNames)
    {
        // In-parameters first, then out-parameters. UI Automation reads a single flat array
        // and splits it by the two counts, so the order is not cosmetic.
        var allTypes = new UIAutomationType[inTypes.Length + outTypes.Length];
        inTypes.CopyTo(allTypes, 0);
        outTypes.CopyTo(allTypes, inTypes.Length);

        if (allTypes.Length != parameterNames.Length)
        {
            throw new InvalidOperationException(
                $"Method '{name}' declares {allTypes.Length} parameters but {parameterNames.Length} "
                + "names. The two arrays are read in parallel by UI Automation.");
        }

        return new UIAutomationMethodInfo
        {
            ProgrammaticName = AllocString(name),
            DoSetFocus = 0,
            InParameterCount = (uint)inTypes.Length,
            OutParameterCount = (uint)outTypes.Length,
            ParameterTypes = AllocTypes(allTypes),
            ParameterNames = AllocStringArray(parameterNames),
        };
    }

    private static IntPtr AllocTypes(UIAutomationType[] types)
    {
        var block = Marshal.AllocHGlobal(sizeof(int) * types.Length);

        for (var i = 0; i < types.Length; i++)
        {
            Marshal.WriteInt32(block, i * sizeof(int), (int)types[i]);
        }

        return block;
    }

    private static IntPtr AllocStringArray(string[] values)
    {
        var block = Marshal.AllocHGlobal(IntPtr.Size * values.Length);

        for (var i = 0; i < values.Length; i++)
        {
            Marshal.WriteIntPtr(block, i * IntPtr.Size, AllocString(values[i]));
        }

        return block;
    }

    private static IntPtr AllocString(string value) => Marshal.StringToHGlobalUni(value);
}

/// <summary>
/// Reads and writes one dispatched call's <c>UIAutomationParameter</c> array.
/// </summary>
/// <remarks>
/// <para>
/// The one place in the bridge where a mistake is silent. Every parameter arrives as a type
/// tag and a <c>void*</c>, and the pointer means different things in the two directions: for
/// an in-parameter it addresses the value, for an out-parameter it addresses the caller's
/// storage that the provider must fill. Reading an out-slot or writing an in-slot corrupts
/// memory belonging to UI Automation rather than raising anything.
/// </para>
/// <para>
/// Strings are the subtle case. Both <c>String</c> and <c>OutString</c> slots hold a
/// <c>BSTR*</c> - a pointer to a BSTR variable, not the BSTR itself - so both directions go
/// through one level of indirection.
/// </para>
/// </remarks>
internal static class NativeParameters
{
    private static readonly int ParameterSize = Marshal.SizeOf<UIAutomationParameter>();

    /// <summary>Reads the parameter at <paramref name="index"/> without interpreting it.</summary>
    internal static UIAutomationParameter At(IntPtr parameters, uint index)
        => Marshal.PtrToStructure<UIAutomationParameter>(parameters + ((int)index * ParameterSize));

    /// <summary>Reads an in-parameter holding an <see cref="int"/>.</summary>
    internal static int ReadInt(IntPtr parameters, uint index)
        => Marshal.ReadInt32(At(parameters, index).Data);

    /// <summary>Reads an in-parameter holding a BSTR.</summary>
    /// <returns>The string, or empty when the caller passed a null BSTR.</returns>
    internal static string ReadString(IntPtr parameters, uint index)
    {
        var bstr = Marshal.ReadIntPtr(At(parameters, index).Data);
        return bstr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringBSTR(bstr);
    }

    /// <summary>
    /// Writes an out-parameter holding a BSTR.
    /// </summary>
    /// <remarks>
    /// The BSTR is allocated here and freed by the caller, which is the COM convention for an
    /// out-parameter and is what UI Automation does on the client's behalf. Do not free it.
    /// </remarks>
    internal static void WriteString(IntPtr parameters, uint index, string value)
        => Marshal.WriteIntPtr(At(parameters, index).Data, Marshal.StringToBSTR(value));

    /// <summary>
    /// Fills one slot of an outgoing parameter array.
    /// </summary>
    /// <remarks>
    /// Used by the client wrapper, which owns the storage the slots point at for the duration
    /// of the call and no longer.
    /// </remarks>
    internal static void Set(IntPtr parameters, int index, UIAutomationType type, IntPtr data)
    {
        var slot = new UIAutomationParameter { Type = type, Data = data };
        Marshal.StructureToPtr(slot, parameters + (index * ParameterSize), fDeleteOld: false);
    }

    /// <summary>Allocates an array of <paramref name="count"/> uninitialised parameter slots.</summary>
    internal static IntPtr Allocate(int count) => Marshal.AllocHGlobal(ParameterSize * count);
}
