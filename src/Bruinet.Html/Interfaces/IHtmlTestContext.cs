using Bruinet.Core.Calls;
using Bruinet.Core.Interfaces;
using Bruinet.Html.Context;

namespace Bruinet.Html.Interfaces;

// R9: implements Core's non-generic ITestContext (unchanged by this work) and Html's own scope,
// not Core's generic ITestContext<T>. See AD-010.

/// <summary>
/// One browser page under test: its driver, timeouts and logger, and the root scope for lookups.
/// </summary>
public interface IHtmlTestContext : ITestContext, IHtmlElementScope
{
    /// <summary>The driver that talks to the browser.</summary>
    IHtmlDriver Driver { get; }

    /// <summary>
    /// A name for this context in logs and messages, or <c>null</c>. Tells several apps in one
    /// scenario apart (?A10).
    /// </summary>
    string? Name { get; }

    /// <summary>When a call that passed is logged as a near-miss.</summary>
    NearMissSettings NearMiss { get; }

    /// <summary>The page's URL now.</summary>
    string CurrentUrl { get; }

    /// <summary>The document title now.</summary>
    string PageTitle { get; }

    /// <summary>Goes forward in the browser history.</summary>
    void GoForward();

    /// <summary>Whether the page is idle now.</summary>
    bool IsIdle();
}
