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

    public bool EnableTracing { get; set; }

    public string? CdpEndpoint { get; set; }
}