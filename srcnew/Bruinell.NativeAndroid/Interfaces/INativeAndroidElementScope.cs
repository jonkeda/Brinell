using Bruinell.Core.Scopes;

namespace Bruinell.NativeAndroid.Interfaces;

// R9: NativeAndroid owns this contract and does not derive from Core's scopes. See AD-010.

/// <summary>
/// Where controls are found: the app, a page, a container, a dialog or a list row. A scope says
/// whether it is ready, asking its parent first, and finds views inside itself in one attempt.
/// </summary>
public interface INativeAndroidElementScope
{
    /// <summary>The test context the scope belongs to.</summary>
    INativeAndroidTestContext Context { get; }

    /// <summary>The strategy a plain string locator uses in this scope: a resource id.</summary>
    LocatorStrategy DefaultLocatorStrategy { get; }

    /// <summary>The page the scope is on; null for the app root.</summary>
    INativeAndroidPage? Page { get; }

    /// <summary>One probe of the scope's readiness, its parents first. Never waits.</summary>
    ScopeReadiness ProbeReadiness();

    /// <summary>Whether the scope is ready now.</summary>
    bool IsReady() => ProbeReadiness().IsReady;

    /// <summary>Waits until the scope is ready, as one call.</summary>
    bool WaitReady(int? timeoutMs = null);

    /// <summary>The first matching view in the scope now, or null. One attempt.</summary>
    INativeAndroidElement? TryFindElement(Locator locator);

    /// <summary>The first matching view in the scope now. One attempt.</summary>
    INativeAndroidElement FindElement(Locator locator);

    /// <summary>The matching views in the scope now. One attempt.</summary>
    IReadOnlyList<INativeAndroidElement> FindElements(Locator locator);
}
