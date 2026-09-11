using System.Runtime.InteropServices;
using Brinell.Uia.Interop;

namespace Brinell.Uia;

/// <summary>
/// Registers the Brinell pattern with UI Automation, once per process.
/// </summary>
/// <remarks>
/// <para>
/// <b>The ids are process-local.</b> <c>RegisterPattern</c> allocates
/// <see cref="PatternId"/> on the spot; the same GUID registered in the app under test and in
/// the test assembly typically yields different numbers, and the numbers change between runs.
/// Nothing may persist or compare them across a process boundary - the GUID is the contract,
/// and each side looks up its own id here.
/// </para>
/// <para>
/// <b>Registering twice is harmless.</b> UI Automation returns the same ids for a GUID it has
/// already seen, so the guard below is about avoiding wasted work and a second rooted handler,
/// not about correctness. It is still a guard, because two threads racing through it would
/// otherwise leak a handler each.
/// </para>
/// <para>
/// <b>Windows only.</b> These sources compile for every platform an app under test targets -
/// the attached property that declares verbs lives in shared XAML and has to exist everywhere -
/// but UI Automation does not, and nothing here is reachable off Windows.
/// </para>
/// </remarks>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public sealed class BrinellPatternRegistration
{
    private const string RegistrarClsid = "6e29fabf-9977-42d1-8d0e-ca7e61ad87e6";

    /// <summary>
    /// Rooted forever. UI Automation holds the native pointer for the life of the process and
    /// will call back into it; letting the managed object be collected turns every later
    /// dispatch into a call through a freed CCW.
    /// </summary>
    private static readonly BrinellAutomationPatternHandler Handler = new();

    private static readonly Lazy<BrinellPatternRegistration> Registration =
        new(Register, LazyThreadSafetyMode.ExecutionAndPublication);

    private BrinellPatternRegistration(int patternId, int isPatternAvailablePropertyId)
    {
        PatternId = patternId;
        IsPatternAvailablePropertyId = isPatternAvailablePropertyId;
    }

    /// <summary>
    /// The registration for this process, performing it on first use.
    /// </summary>
    /// <exception cref="BrinellUiaException">
    /// The registrar could not be created or refused the pattern. On the provider side this
    /// means the app cannot publish the bridge; on the client side it means no test can use it.
    /// Either way it is fatal to the bridge and survivable for everything else, which is why it
    /// is an exception and not a process failure.
    /// </exception>
    public static BrinellPatternRegistration Current => Registration.Value;

    /// <summary>
    /// The registration, or the reason there is none.
    /// </summary>
    /// <remarks>
    /// The form to use in an app under test. A missing bridge must degrade to "this app has no
    /// instrumentation", never to a startup crash - the app has a job of its own.
    /// </remarks>
    /// <param name="registration">The registration, when this returns true.</param>
    /// <param name="error">Why registration failed, when this returns false.</param>
    /// <returns>Whether the pattern is registered in this process.</returns>
    public static bool TryGetCurrent(
        out BrinellPatternRegistration? registration,
        out string? error)
    {
        try
        {
            registration = Registration.Value;
            error = null;
            return true;
        }
        catch (BrinellUiaException ex)
        {
            registration = null;
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// This process's id for the pattern. Compare <c>GetPatternProvider</c>'s argument to this.
    /// </summary>
    public int PatternId { get; }

    /// <summary>
    /// This process's id for the generated <c>IsBrinellAutomationPatternAvailable</c> property.
    /// </summary>
    /// <remarks>
    /// Lets a client ask whether an element carries the pattern without calling into the app,
    /// which is the cheap way to answer "does this app have a bridge at all".
    /// </remarks>
    public int IsPatternAvailablePropertyId { get; }

    private static BrinellPatternRegistration Register()
    {
        var registrar = CreateRegistrar();

        var handlerPointer = Marshal.GetComInterfaceForObject<
            BrinellAutomationPatternHandler, IUIAutomationPatternHandler>(Handler);

        // Never released: see the Handler field. The reference is the process's, not a scope's.
        var info = NativeTable.BuildPatternInfo(handlerPointer);

        var hr = registrar.RegisterPattern(
            ref info,
            out var patternId,
            out var isPatternAvailablePropertyId,
            propertyIdCount: 0,
            propertyIds: null,
            eventIdCount: 0,
            eventIds: null);

        if (hr != HResults.S_OK)
        {
            throw new BrinellUiaException(
                $"UI Automation refused to register pattern {BrinellUiaIds.PatternGuid} "
                + $"(HRESULT 0x{hr:X8}). The method table or one of the interface ids is "
                + "malformed; nothing the caller did at runtime can cause this.");
        }

        return new BrinellPatternRegistration(patternId, isPatternAvailablePropertyId);
    }

    private static IUIAutomationRegistrar CreateRegistrar()
    {
        var registrarType = Type.GetTypeFromCLSID(new Guid(RegistrarClsid));

        if (registrarType is null)
        {
            throw new BrinellUiaException(
                $"No type for CUIAutomationRegistrar (CLSID {{{RegistrarClsid}}}). "
                + "UI Automation is not available in this process.");
        }

        object? instance;
        try
        {
            instance = Activator.CreateInstance(registrarType);
        }
        catch (Exception ex)
        {
            throw new BrinellUiaException(
                "Could not create CUIAutomationRegistrar. Custom UI Automation patterns are "
                + "unavailable in this process.", ex);
        }

        if (instance is IUIAutomationRegistrar registrar)
        {
            return registrar;
        }

        // Query explicitly rather than reporting that the cast failed. A cast tells you only
        // that it did not work; the HRESULT distinguishes a wrong IID in this file
        // (E_NOINTERFACE) from an apartment or activation problem, and those have nothing to do
        // with one another.
        var unknown = Marshal.GetIUnknownForObject(instance!);
        try
        {
            var iid = typeof(IUIAutomationRegistrar).GUID;
            var hr = Marshal.QueryInterface(unknown, in iid, out _);

            throw new BrinellUiaException(
                $"CUIAutomationRegistrar does not answer to IUIAutomationRegistrar {{{iid}}} "
                + $"(QueryInterface returned 0x{hr:X8}). The interface declaration in this "
                + "assembly and the installed UIAutomationCore disagree.");
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }
}

/// <summary>Something in the UI Automation bridge failed in a way the caller can report.</summary>
/// <remarks>
/// Declared here rather than in <c>Brinell.Core</c> deliberately: these sources are compiled
/// into the app under test, which must be able to carry the bridge without taking a dependency
/// on the test framework. See the contracts project file.
/// </remarks>
public sealed class BrinellUiaException : Exception
{
    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What failed, and what it means for the caller.</param>
    public BrinellUiaException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and the failure underneath it.</summary>
    /// <param name="message">What failed, and what it means for the caller.</param>
    /// <param name="innerException">The underlying failure.</param>
    public BrinellUiaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
