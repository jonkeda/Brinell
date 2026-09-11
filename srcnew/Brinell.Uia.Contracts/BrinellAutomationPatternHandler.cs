using System.Runtime.InteropServices;
using Brinell.Uia.Interop;

namespace Brinell.Uia;

/// <summary>
/// Translates between UI Automation's index-and-parameter-array calling convention and the
/// two methods of the contract.
/// </summary>
/// <remarks>
/// <para>
/// One class, both directions, because they have to agree.
/// <see cref="CreateClientWrapper"/> runs in the test assembly and
/// <see cref="Dispatch"/> runs in the app under test, and the parameter array one builds is
/// the parameter array the other reads. Splitting them across two files is how they drift.
/// </para>
/// <para>
/// <b>The index switch is the other half of the frozen method table.</b> The cases here must
/// match <see cref="NativeTable"/> exactly - not by name, by number.
/// </para>
/// </remarks>
[ComVisible(true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class BrinellAutomationPatternHandler : IUIAutomationPatternHandler
{
    /// <inheritdoc/>
    public int CreateClientWrapper(
        IUIAutomationPatternInstance patternInstance,
        [MarshalAs(UnmanagedType.IUnknown)] out object? clientWrapper)
    {
        try
        {
            clientWrapper = new BrinellAutomationPatternClient(patternInstance);
            return HResults.S_OK;
        }
        catch (Exception)
        {
            // UI Automation calls this while resolving a pattern the caller asked for. Failing
            // it means "no such pattern here", which is a survivable answer; letting the
            // exception out is not.
            clientWrapper = null;
            return HResults.E_FAIL;
        }
    }

    /// <inheritdoc/>
    public int Dispatch(
        [MarshalAs(UnmanagedType.IUnknown)] object target,
        uint index,
        IntPtr parameters,
        uint parameterCount)
    {
        if (target is not IBrinellAutomationProvider provider)
        {
            return HResults.E_INVALIDARG;
        }

        try
        {
            return index switch
            {
                NativeTable.InvokeMethodIndex => DispatchInvoke(provider, parameters, parameterCount),
                NativeTable.ExchangeMethodIndex => DispatchExchange(provider, parameters, parameterCount),

                // A client built from a newer contract than this provider. Reported, not crashed.
                _ => HResults.UIA_E_NOTSUPPORTED,
            };
        }
        catch (Exception)
        {
            // This is the COM boundary. Whatever went wrong - a bad pointer, a provider that
            // threw despite the rule - stops here.
            return HResults.E_FAIL;
        }
    }

    private static int DispatchInvoke(
        IBrinellAutomationProvider provider, IntPtr parameters, uint parameterCount)
    {
        // Three in, none out. A mismatch means the two ends disagree about the method table,
        // and reading the array anyway would read past it.
        if (parameterCount != 3)
        {
            return HResults.E_INVALIDARG;
        }

        return provider.Invoke(
            NativeParameters.ReadInt(parameters, 0),
            NativeParameters.ReadInt(parameters, 1),
            NativeParameters.ReadInt(parameters, 2));
    }

    private static int DispatchExchange(
        IBrinellAutomationProvider provider, IntPtr parameters, uint parameterCount)
    {
        // Two in, one out.
        if (parameterCount != 3)
        {
            return HResults.E_INVALIDARG;
        }

        var hr = provider.Exchange(
            NativeParameters.ReadInt(parameters, 0),
            NativeParameters.ReadString(parameters, 1),
            out var result);

        // Written even on failure. The caller's BSTR slot is uninitialised until something
        // puts a value in it, and a client that reads it on the failure path would otherwise
        // be reading whatever was on that stack.
        NativeParameters.WriteString(parameters, 2, result ?? string.Empty);

        return hr;
    }
}

/// <summary>
/// What <c>GetCurrentPattern</c> hands back in the test assembly.
/// </summary>
/// <remarks>
/// Packs each call into the parameter array UI Automation expects, and unpacks the result.
/// Every allocation made here is released before the method returns, except the result BSTR,
/// which the provider allocated and this side frees - the ordinary COM out-parameter rule.
/// </remarks>
[ComVisible(true)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
internal sealed class BrinellAutomationPatternClient : IBrinellAutomationPattern
{
    private readonly IUIAutomationPatternInstance _instance;

    internal BrinellAutomationPatternClient(IUIAutomationPatternInstance instance)
        => _instance = instance;

    /// <inheritdoc/>
    public int Invoke(int verb, int arg1, int arg2)
    {
        var parameters = NativeParameters.Allocate(3);
        var storage = Marshal.AllocHGlobal(sizeof(int) * 3);

        try
        {
            Marshal.WriteInt32(storage, 0 * sizeof(int), verb);
            Marshal.WriteInt32(storage, 1 * sizeof(int), arg1);
            Marshal.WriteInt32(storage, 2 * sizeof(int), arg2);

            NativeParameters.Set(parameters, 0, UIAutomationType.Int, storage);
            NativeParameters.Set(parameters, 1, UIAutomationType.Int, storage + sizeof(int));
            NativeParameters.Set(parameters, 2, UIAutomationType.Int, storage + (2 * sizeof(int)));

            return _instance.CallMethod(NativeTable.InvokeMethodIndex, parameters, 3);
        }
        finally
        {
            Marshal.FreeHGlobal(storage);
            Marshal.FreeHGlobal(parameters);
        }
    }

    /// <inheritdoc/>
    public int Exchange(int verb, string argument, out string result)
    {
        result = string.Empty;

        var parameters = NativeParameters.Allocate(3);
        var verbStorage = Marshal.AllocHGlobal(sizeof(int));

        // Both string slots hold a BSTR*, so each needs a pointer-sized cell of its own to
        // point at - the BSTR itself is not what goes in the parameter array.
        var argumentSlot = Marshal.AllocHGlobal(IntPtr.Size);
        var resultSlot = Marshal.AllocHGlobal(IntPtr.Size);
        var argumentBstr = Marshal.StringToBSTR(argument ?? string.Empty);

        try
        {
            Marshal.WriteInt32(verbStorage, verb);
            Marshal.WriteIntPtr(argumentSlot, argumentBstr);
            Marshal.WriteIntPtr(resultSlot, IntPtr.Zero);

            NativeParameters.Set(parameters, 0, UIAutomationType.Int, verbStorage);
            NativeParameters.Set(parameters, 1, UIAutomationType.String, argumentSlot);
            NativeParameters.Set(parameters, 2, UIAutomationType.OutString, resultSlot);

            var hr = _instance.CallMethod(NativeTable.ExchangeMethodIndex, parameters, 3);

            var resultBstr = Marshal.ReadIntPtr(resultSlot);
            if (resultBstr != IntPtr.Zero)
            {
                result = Marshal.PtrToStringBSTR(resultBstr);
                Marshal.FreeBSTR(resultBstr);
            }

            return hr;
        }
        finally
        {
            Marshal.FreeBSTR(argumentBstr);
            Marshal.FreeHGlobal(resultSlot);
            Marshal.FreeHGlobal(argumentSlot);
            Marshal.FreeHGlobal(verbStorage);
            Marshal.FreeHGlobal(parameters);
        }
    }
}
