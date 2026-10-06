using Brinell.Core.Calls;
using Brinell.Core.Configuration;
using Brinell.Core.Logging;

namespace Brinell.Html.Context;

public class HtmlTestContextOptions
{
    public string? BaseUrl { get; set; }

    public bool Headless { get; set; } = true;

    public string BrowserType { get; set; } = "chromium";

    public TimeoutSettings Timeouts { get; set; } = TimeoutSettings.Default;

    public ITestLogger? Logger { get; set; }

    /// <summary>A name for the context in logs and messages; see <c>IHtmlTestContext.Name</c>.</summary>
    public string? Name { get; set; }

    /// <summary>When a call that passed is logged as a near-miss.</summary>
    public NearMissSettings NearMiss { get; set; } = NearMissSettings.Default;

    public bool EnableTracing { get; set; }

    public string? CdpEndpoint { get; set; }
}